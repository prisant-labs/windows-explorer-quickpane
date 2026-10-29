using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using QuickPane.Services;
using NM = QuickPane.Interop.NativeMethods;

namespace QuickPane.Interop
{
    /// <summary>
    /// Drives a specific Explorer window from outside its process. It uses the Shell.Application
    /// automation object to find the ShellBrowserWindow whose top-level HWND matches the window the
    /// pane is embedded in, then navigates it in place, which is equivalent to the user clicking a
    /// folder in the native nav pane.
    ///
    /// Every call here is late bound through Type.InvokeMember rather than the C# dynamic keyword.
    /// The dynamic path routes COM calls through the DLR, which caches a wrapper against each runtime
    /// callable wrapper using Marshal.SetComObjectData, and that cache is invalidated by the explicit
    /// Marshal.ReleaseComObject calls this class needs to make. The two together threw
    /// "InvalidOperationException: Marshal.SetComObjectData failed" out of ComObject.ObjectToComObject,
    /// which is what silently dropped folder clicks and Recent updates. InvokeMember reaches IDispatch
    /// directly, so no such cache exists and releasing an object cannot poison a later call.
    /// </summary>
    internal static class ExplorerNavigator
    {
        /// <summary>Navigate on the background worker. Enumerating shell windows is cross-process COM
        /// that blocks for as long as Explorer is busy, and the UI thread shares Explorer's input queue
        /// in Inside mode, so this must never run there.</summary>
        public static void NavigateAsync(IntPtr topLevelHwnd, string path)
        {
            WorkQueue.Post(() => Navigate(topLevelHwnd, path));
        }

        // 0x800700AA is ERROR_BUSY surfaced through COM: the window is mid-operation rather than wrong
        // or gone, so the call is worth making again once the shell has had a moment.
        private const int HResultResourceInUse = unchecked((int)0x800700AA);

        /// <summary>Drive Navigate2, retrying once when the shell reports the window busy. Giving up on
        /// a busy window left the click doing nothing at all.</summary>
        private static bool Navigate2WithRetry(object w, string path)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    Call(w, "Navigate2", path);
                    return true;
                }
                catch (Exception ex) when (attempt == 0 && IsBusy(ex))
                {
                    System.Threading.Thread.Sleep(250);
                }
            }
        }

        private static bool IsBusy(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
                if (System.Runtime.InteropServices.Marshal.GetHRForException(e) == HResultResourceInUse)
                    return true;
            return false;
        }

        /// <summary>Navigate the Explorer window identified by topLevelHwnd to path, in place.</summary>
        public static bool Navigate(IntPtr topLevelHwnd, string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            Log.Activity("explorer navigate to " + path);

            object shell = null, windows = null;
            try
            {
                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null)
                {
                    Log.Event("navigate", "Shell.Application is not registered, so in-place navigation is unavailable.");
                    return false;
                }
                shell = Activator.CreateInstance(shellType);
                windows = Call(shell, "Windows");
                if (windows == null) return false;

                object w = null;
                try
                {
                    w = FindActiveTab(windows, topLevelHwnd);
                    if (w == null)
                    {
                        Log.Event("navigate", "no Explorer window matched hwnd " + topLevelHwnd.ToString("X") +
                            ", so '" + path + "' was not opened in place.");
                        return false;
                    }
                    return Navigate2WithRetry(w, path);
                }
                catch (Exception ex)
                {
                    Log.Event("navigate", "Navigate2 failed for '" + path + "'", ex);
                    return false;
                }
                finally { Release(w); }
            }
            catch (Exception ex)
            {
                Log.Event("navigate", "in-place navigation failed for '" + path + "'", ex);
                return false;
            }
            finally
            {
                Release(windows);
                Release(shell);
            }
        }

        /// <summary>The folder currently shown in the Explorer window with this top-level HWND, or null.</summary>
        public static string GetCurrentPath(IntPtr topLevelHwnd)
        {
            object shell = null, windows = null;
            try
            {
                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return null;
                shell = Activator.CreateInstance(shellType);
                windows = Call(shell, "Windows");
                if (windows == null) return null;

                object w = null;
                try
                {
                    w = FindActiveTab(windows, topLevelHwnd);
                    return w == null ? null : PathOf(w);
                }
                finally { Release(w); }
            }
            catch (Exception ex) { Log.Error("GetCurrentPath failed", ex); return null; }
            finally
            {
                Release(windows);
                Release(shell);
            }
        }

        /// <summary>
        /// The shell window for the tab in front of the Explorer window with this top-level HWND, or
        /// null. The caller releases it.
        ///
        /// Shell.Application.Windows() lists one entry per Windows 11 tab, and every tab reports the
        /// top-level CabinetWClass handle as its HWND, so taking the first match drove the oldest tab
        /// whichever tab was in front (prisant-labs/windows-explorer-quickpane#35). Each tab's own
        /// ShellTabWindowClass handle comes from its IShellBrowser, and the active tab is the topmost
        /// ShellTabWindowClass child. ExplorerWindow lays the tabs out with SWP_NOZORDER, so it never
        /// disturbs that order. A lone match, which is every Windows 10 window and every single-tab
        /// window, skips the tab lookup entirely.
        /// </summary>
        private static object FindActiveTab(object windows, IntPtr topLevelHwnd)
        {
            long target = topLevelHwnd.ToInt64();
            var matches = new List<object>();
            try
            {
                int count = ToInt(Get(windows, "Count"));
                for (int i = 0; i < count; i++)
                {
                    object w = null;
                    try { w = Call(windows, "Item", i); }
                    catch { }
                    if (w == null) continue;
                    if (HwndOf(w) == target) matches.Add(w);
                    else Release(w);
                }
                if (matches.Count == 0) return null;

                int pick = 0;
                if (matches.Count > 1)
                {
                    long active = NM.FindWindowEx(topLevelHwnd, IntPtr.Zero, "ShellTabWindowClass", null).ToInt64();
                    var tabs = new long[matches.Count];
                    for (int i = 0; i < tabs.Length; i++) tabs[i] = TabHwndOf(matches[i]);
                    pick = PickActiveTab(tabs, active);
                }
                var chosen = matches[pick];
                matches[pick] = null;   // handed to the caller, so the finally block must not release it
                return chosen;
            }
            finally
            {
                foreach (var m in matches) Release(m);
            }
        }

        /// <summary>Which of the shell windows sharing one top-level HWND to drive, given each one's
        /// tab handle (0 where the lookup failed) and the active tab's handle (0 when unknown). Falls
        /// back to the first, which is what every build before #35 did, so a failed lookup is never
        /// worse than before. Returns -1 only for an empty list.</summary>
        internal static int PickActiveTab(IList<long> tabHandles, long activeTab)
        {
            if (tabHandles == null || tabHandles.Count == 0) return -1;
            if (activeTab != 0)
                for (int i = 0; i < tabHandles.Count; i++)
                    if (tabHandles[i] == activeTab) return i;
            return 0;
        }

        /// <summary>The ShellTabWindowClass handle of one tab's shell window, or 0. This is the
        /// documented route from a shell window to its browser: QueryService for the top-level
        /// browser, then IOleWindow.GetWindow, which IShellBrowser inherits. Both interfaces marshal
        /// across processes, so this works from QuickPane even though the tab lives in Explorer.</summary>
        private static long TabHwndOf(object w)
        {
            object browser = null;
            try
            {
                var sp = w as IOleServiceProvider;
                if (sp == null) return 0;
                Guid sid = SidSTopLevelBrowser, iid = IidIShellBrowser;
                if (sp.QueryService(ref sid, ref iid, out browser) != 0) return 0;
                var sb = browser as IShellBrowser;
                if (sb == null) return 0;
                IntPtr hwnd;
                return sb.GetWindow(out hwnd) == 0 ? hwnd.ToInt64() : 0;
            }
            catch { return 0; }
            finally { Release(browser); }
        }

        private static readonly Guid SidSTopLevelBrowser = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");
        private static readonly Guid IidIShellBrowser = new Guid("000214E2-0000-0000-C000-000000000046");

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);
        }

        // Only the first vtable slot is declared. GetWindow comes from IOleWindow, which IShellBrowser
        // extends, and nothing here calls the later methods.
        [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellBrowser
        {
            [PreserveSig]
            int GetWindow(out IntPtr phwnd);
        }

        /// <summary>Read the folder path from a shell window, preferring the folder object and falling
        /// back to the location URL for shells that return nothing from the first route.</summary>
        private static string PathOf(object w)
        {
            object doc = null, folder = null, self = null;
            try
            {
                doc = Get(w, "Document");
                if (doc != null)
                {
                    folder = Get(doc, "Folder");
                    if (folder != null)
                    {
                        self = Get(folder, "Self");
                        if (self != null)
                        {
                            var p = Get(self, "Path") as string;
                            if (!string.IsNullOrEmpty(p)) return p;
                        }
                    }
                }
            }
            catch { }
            finally { Release(self); Release(folder); Release(doc); }

            try
            {
                var url = Get(w, "LocationURL") as string;   // e.g. file:///C:/Users/...
                if (!string.IsNullOrEmpty(url))
                {
                    var u = new Uri(url);
                    if (u.IsFile) return u.LocalPath;
                }
            }
            catch { }
            return null;
        }

        /// <summary>Open a folder in a brand new Explorer window (used by "Open in new window").</summary>
        public static void OpenNewWindow(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + path + "\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Event("navigate", "opening '" + path + "' in a new window failed", ex);
            }
        }

        // ---- late-bound COM helpers -----------------------------------------

        private const BindingFlags CallFlags = BindingFlags.InvokeMethod;
        private const BindingFlags GetFlags = BindingFlags.GetProperty;

        private static object Call(object target, string member, params object[] args)
        {
            if (target == null) return null;
            return target.GetType().InvokeMember(member, CallFlags, null, target, args);
        }

        private static object Get(object target, string member)
        {
            if (target == null) return null;
            return target.GetType().InvokeMember(member, GetFlags, null, target, null);
        }

        /// <summary>The window handle of a shell window. Items that are not Explorer windows, such as an
        /// Internet Explorer frame, throw here rather than returning a handle.</summary>
        private static long HwndOf(object w)
        {
            try { return Convert.ToInt64(Get(w, "HWND")); }
            catch { return 0; }
        }

        private static int ToInt(object v)
        {
            try { return v == null ? 0 : Convert.ToInt32(v); }
            catch { return 0; }
        }

        private static void Release(object o)
        {
            try { if (o != null && Marshal.IsComObject(o)) Marshal.ReleaseComObject(o); }
            catch { }
        }
    }
}
