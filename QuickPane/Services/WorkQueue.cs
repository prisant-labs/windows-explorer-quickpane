using System;
using System.Collections.Concurrent;
using System.Threading;

namespace QuickPane.Services
{
    /// <summary>
    /// Background STA workers for everything that can touch a slow disk, the network, or
    /// cross-process COM: shortcut resolution, Shell.Application navigation, icon extraction, and
    /// existence probes. This matters because in Inside mode the UI thread is input-attached to
    /// Explorer (and to any app hosting a file dialog we embedded in), so a single blocking call on
    /// the UI thread stalls input for the whole shell even though CPU stays idle. Posting here means
    /// the only threads that ever wait on I/O are these.
    ///
    /// Folder clicks get a lane of their own. They used to share the one worker with the Recent
    /// poll, path probes, drag-and-drop copies, and the update check, so a click waited for whatever
    /// was running, for seconds behind a large copy or a dead network share
    /// (prisant-labs/windows-explorer-quickpane#38).
    /// </summary>
    internal static class WorkQueue
    {
        private static readonly WorkLane General = new WorkLane("QuickPaneWorker");
        private static readonly WorkLane Navigation = new WorkLane("QuickPaneNavigator");

        /// <summary>Queue work on the general worker. Never blocks the caller.</summary>
        public static void Post(Action work) { General.Post(work); }

        /// <summary>Queue a navigation the user asked for: driving an Explorer window or a file
        /// dialog to a folder. Never blocks the caller. Keep anything else off this lane, so a click
        /// only ever waits for an earlier click.</summary>
        public static void PostNavigation(Action work) { Navigation.Post(work); }

        /// <summary>Run on the WPF UI thread without waiting for it.</summary>
        public static void PostUI(Action work)
        {
            if (work == null) return;
            var app = System.Windows.Application.Current;
            var disp = app != null ? app.Dispatcher : null;
            if (disp == null || disp.HasShutdownStarted) return;
            disp.BeginInvoke(work);
        }
    }

    /// <summary>
    /// One background STA thread that runs posted work in order. The thread starts on the first
    /// post, and a failing item is reported without stopping the ones behind it. Failures go to the
    /// log unless a test passes its own handler.
    /// </summary>
    internal sealed class WorkLane
    {
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly object _gate = new object();
        private readonly string _name;
        private readonly Action<string, Exception> _onError;
        private Thread _thread;

        public WorkLane(string name, Action<string, Exception> onError = null)
        {
            _name = name;
            _onError = onError ?? Log.Error;
        }

        public void Post(Action work)
        {
            if (work == null) return;
            EnsureThread();
            try { _queue.Add(work); }
            catch (InvalidOperationException) { /* shutting down */ }
        }

        private void EnsureThread()
        {
            if (_thread != null) return;
            lock (_gate)
            {
                if (_thread != null) return;
                var t = new Thread(Loop) { IsBackground = true, Name = _name };
                // STA so the shell COM used here (IShellLink, Shell.Application, SHGetFileInfo)
                // behaves the way it does on the UI thread.
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                _thread = t;
            }
        }

        private void Loop()
        {
            foreach (var work in _queue.GetConsumingEnumerable())
            {
                try { work(); }
                catch (Exception ex) { _onError("background work failed on " + _name, ex); }
            }
        }
    }
}
