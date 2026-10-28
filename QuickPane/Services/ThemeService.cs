using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using NM = QuickPane.Interop.NativeMethods;

namespace QuickPane.Services
{
    /// <summary>
    /// Owns the active theme. Reads AppsUseLightTheme and the accent color from the registry, loads the
    /// matching ResourceDictionary, and overrides the accent brushes from the live system accent.
    /// SystemEvents.UserPreferenceChanged fires when the user flips dark/light or changes accent, so
    /// the sidebar restyles with no restart.
    /// </summary>
    public sealed class ThemeService : IDisposable
    {
        public event Action ThemeChanged;

        public bool IsDark { get; private set; }
        public Color Accent { get; private set; } = Color.FromArgb(0xFF, 0x00, 0x78, 0xD4);

        /// <summary>A Windows contrast theme is on. Every brush then comes from the system colours the
        /// person chose, not from the light or dark dictionary (#30).</summary>
        public bool IsHighContrast { get; private set; }

        private const string PersonalizeKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        // The accent is not under Personalize, which holds only the mode and transparency switches.
        // Reading it there always missed, so the pane kept its default blue (#20). DWM holds the
        // accent itself; Explorer\Accent keeps a copy for the Start menu.
        private const string DwmKey = @"SOFTWARE\Microsoft\Windows\DWM";
        private const string ExplorerAccentKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Accent";
        private ResourceDictionary _current;

        public void Start()
        {
            ReadRegistry();
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        public void Apply()
        {
            var app = Application.Current;
            if (app == null) return;

            var uri = IsDark
                ? new Uri("/QuickPane;component/Themes/Theme.Dark.xaml", UriKind.Relative)
                : new Uri("/QuickPane;component/Themes/Theme.Light.xaml", UriKind.Relative);

            ResourceDictionary dict;
            try
            {
                dict = (ResourceDictionary)Application.LoadComponent(uri);
            }
            catch (Exception ex)
            {
                Log.Error("theme dictionary load failed", ex);
                return;
            }

            if (IsHighContrast)
            {
                // The light or dark dictionary still supplies the control styles it merges; only its
                // brushes give way to the contrast colours.
                var palette = HighContrastPalette(SysColor(System.Drawing.SystemColors.Window),
                    SysColor(System.Drawing.SystemColors.WindowText), SysColor(System.Drawing.SystemColors.Highlight),
                    SysColor(System.Drawing.SystemColors.HighlightText), SysColor(System.Drawing.SystemColors.GrayText));
                foreach (var kv in palette)
                {
                    var b = new SolidColorBrush(kv.Value); b.Freeze();
                    dict[kv.Key] = b;
                }
            }
            else
            {
                // Override accent brushes from the live system accent so it always matches Windows.
                var accentBrush = new SolidColorBrush(Accent); accentBrush.Freeze();
                var accentSoft = new SolidColorBrush(WithAlpha(Accent, 0x33)); accentSoft.Freeze();
                var accentHover = new SolidColorBrush(WithAlpha(Accent, 0x26)); accentHover.Freeze();
                dict["AccentBrush"] = accentBrush;
                dict["ItemActiveBackground"] = accentSoft;
                dict["ItemHoverBackground"] = accentHover;
            }

            var merged = app.Resources.MergedDictionaries;
            if (_current != null) merged.Remove(_current);
            merged.Add(dict);
            _current = dict;

            ThemeChanged?.Invoke();
        }

        /// <summary>Windows hosted as HwndSource roots fall back to Application resources, so brushes
        /// need nothing here. The title bar does: Windows draws it and keeps it light unless asked, so
        /// a dark Settings window had a white one (#26). Windows without a standard frame (the panes,
        /// the dock, the borderless prompts) draw their own chrome and are left alone.</summary>
        public void AttachWindow(Window window)
        {
            if (window == null || window.WindowStyle == WindowStyle.None) return;
            Action apply = () => ApplyTitleBar(window);
            window.SourceInitialized += (s, e) => apply();
            ThemeChanged += apply;
            window.Closed += (s, e) => ThemeChanged -= apply;
            apply();   // a no-op until the window has a handle; SourceInitialized covers that case
        }

        private void ApplyTitleBar(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            int dark = IsDark ? 1 : 0;
            if (NM.DwmSetWindowAttribute(hwnd, NM.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int)) != 0)
                NM.DwmSetWindowAttribute(hwnd, NM.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref dark, sizeof(int));
            // The title bar of a window already on screen keeps its old colours until the frame is
            // redrawn, so a theme switch would show only after the next activation without this.
            NM.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                NM.SWP_NOMOVE | NM.SWP_NOSIZE | NM.SWP_NOZORDER | NM.SWP_NOACTIVATE | NM.SWP_FRAMECHANGED);
        }

        private void ReadRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey))
                {
                    var light = key?.GetValue("AppsUseLightTheme");
                    IsDark = light is int && (int)light == 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error("theme registry read failed", ex);
                IsDark = false;
            }

            var accent = ParseAccent(ReadValue(DwmKey, "AccentColor"))
                ?? ParseAccent(ReadValue(ExplorerAccentKey, "AccentColorMenu"));
            if (accent.HasValue) Accent = accent.Value;

            // Asked of Windows each time rather than through WPF's SystemParameters and SystemColors,
            // which cache and may not have refreshed yet when this runs after a contrast switch.
            IsHighContrast = System.Windows.Forms.SystemInformation.HighContrast;
            // A contrast theme sets its own light or dark background; the title bar and the windows
            // that pick a dark or light fill follow that instead of the app mode switch.
            if (IsHighContrast) IsDark = IsDarkColor(SysColor(System.Drawing.SystemColors.Window));
        }

        /// <summary>Every theme brush in a contrast theme, from the system colours. Contrast themes have
        /// no secondary text colour (GrayText means disabled), so secondary text, rules and chevrons all
        /// take WindowText. Hover and selection tint the background with Highlight rather than fill it,
        /// so the WindowText on top stays readable; a full Highlight fill carries HighlightText.</summary>
        internal static System.Collections.Generic.Dictionary<string, Color> HighContrastPalette(
            Color window, Color windowText, Color highlight, Color highlightText, Color grayText)
        {
            return new System.Collections.Generic.Dictionary<string, Color>
            {
                { "SidebarBackground", window },
                { "TextPrimary", windowText },
                { "TextSecondary", windowText },
                { "SeparatorColor", windowText },
                { "ChevronColor", windowText },
                { "DriveBarTrack", grayText },
                { "BrokenLinkColor", grayText },
                { "FieldBackground", window },
                { "FieldBorder", windowText },
                { "BadgeBackground", WithAlpha(highlight, 0x66) },
                { "AccentBrush", highlight },
                { "ItemHoverBackground", WithAlpha(highlight, 0x40) },
                { "ItemActiveBackground", WithAlpha(highlight, 0x66) },
                { "TextOnAccent", highlightText },
            };
        }

        internal static bool IsDarkColor(Color c)
        {
            return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) < 128;
        }

        private static Color SysColor(System.Drawing.Color c)
        {
            return Color.FromRgb(c.R, c.G, c.B);
        }

        private static object ReadValue(string keyPath, string name)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                    return key?.GetValue(name);
            }
            catch (Exception ex)
            {
                Log.Error("theme registry read failed: " + name, ex);
                return null;
            }
        }

        /// <summary>The accent from a registry value, or null when the value is missing or is not a
        /// DWORD. The registry hands a DWORD back as a signed int.</summary>
        internal static Color? ParseAccent(object value)
        {
            if (!(value is int)) return null;
            return AbgrToColor(unchecked((uint)(int)value));
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            // Accessibility is the category a contrast theme switch arrives as.
            if (e.Category != UserPreferenceCategory.General &&
                e.Category != UserPreferenceCategory.Color &&
                e.Category != UserPreferenceCategory.VisualStyle &&
                e.Category != UserPreferenceCategory.Accessibility)
                return;

            var app = Application.Current;
            if (app == null) return;
            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                ReadRegistry();
                Apply();
            }));
        }

        // Registry AccentColor is 0xAABBGGRR; WPF wants ARGB.
        internal static Color AbgrToColor(uint v)
        {
            byte a = (byte)((v >> 24) & 0xFF);
            byte b = (byte)((v >> 16) & 0xFF);
            byte g = (byte)((v >> 8) & 0xFF);
            byte r = (byte)(v & 0xFF);
            if (a == 0) a = 0xFF;
            return Color.FromArgb(a, r, g, b);
        }

        private static Color WithAlpha(Color c, byte a)
        {
            return Color.FromArgb(a, c.R, c.G, c.B);
        }

        public void Dispose()
        {
            try { SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged; } catch { }
        }
    }
}
