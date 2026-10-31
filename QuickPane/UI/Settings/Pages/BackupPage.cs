using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.Win32;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #93 (Keyboard and Backup & restore pages). Covers AC-15, AC-17. See the README's
    // "Settings inventory" for every control this page must carry over from
    // SettingsPanel.xaml.cs's "Backup" tab, and the old code at commit 068301b. The old footer's
    // version/update-check UI does NOT move here; it moves to About (#95, AC-18).

    /// <summary>Back up and Restore buttons, with the result in an info bar. A successful restore
    /// calls <see cref="SettingsPageContext.Rebuild"/>, as today (AC-17).
    ///
    /// <see cref="SettingsStore.ExportTo"/> and <see cref="SettingsStore.ImportFrom"/> run
    /// synchronously on the UI thread, exactly like the old handlers at 068301b, and like every
    /// other settings.json writer in the app today -- not posted through <c>WorkQueue</c>.
    /// <see cref="SettingsStore.Save"/> raises <c>Changed</c>, whose handler in
    /// <c>App.xaml.cs</c> calls <c>Groups.Reload()</c> inline (not deferred), and
    /// <see cref="SettingsStore.ImportFrom"/> replaces <c>Current</c> itself before saving: both
    /// assume the thread that owns the window and the rest of the app's state, exactly the UI
    /// thread they already run on for every other caller. Moving either call to a
    /// <c>WorkQueue</c> worker would fire those handlers off-thread and let a background restore
    /// swap <c>Current</c> out from under a UI-thread reader -- a correctness regression AC-23
    /// does not ask for, since a settings.json file is a few KB of local JSON, not the SSH or
    /// backup-of-a-large-file case AC-23 is aimed at. If a future change needs this off the UI
    /// thread, <c>SettingsStore</c> itself needs a worker-safe deserialize step separate from the
    /// UI-thread adopt (Current swap + Save + event raise); that split is not made here.</summary>
    internal sealed class BackupPage : SettingsPage
    {
        private const string CardId = "backup-restore";

        // One-shot: set just before a successful restore calls ctx.Rebuild(), so the fresh Build()
        // that follows can show "Settings restored." even though nothing else about a page persists
        // state across a rebuild (the contract's caching rule is about settings values, not a single
        // just-happened status message). Cleared as soon as it is read.
        private string _pendingRestoreMessage;

        public override string Id => "backup";
        public override string Title => "Backup & restore";
        public override string Glyph => "";   // Segoe Fluent Icons UpdateRestore

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get { yield return new SettingsSearchEntry(Id, CardId, Title, "Export or import every setting as one file.", "backup", "restore", "export", "import"); }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var settings = ctx.Settings;

            var statusHost = new ContentControl { Focusable = false, Margin = new Thickness(0, 8, 0, 0) };
            if (_pendingRestoreMessage != null)
            {
                statusHost.Content = ctx.Cards.InfoBar("Settings restored", _pendingRestoreMessage, InfoBarSeverity.Success);
                _pendingRestoreMessage = null;
            }

            var backupButton = new Button { Content = "Back up…" };
            AutomationProperties.SetName(backupButton, "Back up settings to a file");
            var restoreButton = new Button { Content = "Restore…", Margin = new Thickness(8, 0, 0, 0) };
            AutomationProperties.SetName(restoreButton, "Restore settings from a file");

            backupButton.Click += (s, e) =>
            {
                var dlg = new SaveFileDialog { FileName = "QuickPane-settings.json", Filter = "JSON file (*.json)|*.json|All files (*.*)|*.*" };
                if (dlg.ShowDialog() != true) return;

                try
                {
                    settings.ExportTo(dlg.FileName);
                    statusHost.Content = ctx.Cards.InfoBar("Backed up", BackedUpMessage(dlg.FileName), InfoBarSeverity.Success);
                }
                catch (Exception ex)
                {
                    statusHost.Content = ctx.Cards.InfoBar("Backup failed", BackupFailedMessage(ex.Message), InfoBarSeverity.Error);
                }
            };

            restoreButton.Click += (s, e) =>
            {
                var dlg = new OpenFileDialog { Filter = "JSON file (*.json)|*.json|All files (*.*)|*.*" };
                if (dlg.ShowDialog() != true) return;

                if (settings.ImportFrom(dlg.FileName))
                {
                    // Rebuilds the whole window on this same page (AC-17), exactly as the old
                    // handler's BuildUI() call did.
                    _pendingRestoreMessage = RestoredMessage;
                    ctx.Rebuild();
                }
                else
                {
                    statusHost.Content = ctx.Cards.InfoBar("Restore failed", RestoreFailedMessage, InfoBarSeverity.Error);
                }
            };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal };
            buttons.Children.Add(backupButton);
            buttons.Children.Add(restoreButton);

            var body = new StackPanel();
            body.Children.Add(buttons);
            body.Children.Add(statusHost);

            return ctx.Cards.Card(CardId, Glyph, "Back up or restore settings",
                "Save every setting to one file, or replace them all from one.", body,
                "Back up or restore settings");
        }

        /// <summary>Matches the old status text verbatim (<c>SettingsPanel.xaml.cs</c>'s
        /// <c>BackupBlock</c> at 068301b: <c>"Backed up to " + dlg.FileName</c>). A pure decision,
        /// so it is unit-testable on its own (AC-27).</summary>
        internal static string BackedUpMessage(string path) => "Backed up to " + path;

        /// <summary>Matches the old status text verbatim (<c>"Backup failed: " + ex.Message</c>).</summary>
        internal static string BackupFailedMessage(string exceptionMessage) => "Backup failed: " + exceptionMessage;

        /// <summary>Matches the old status text verbatim for a successful restore
        /// (<c>"Settings restored."</c>).</summary>
        internal const string RestoredMessage = "Settings restored.";

        /// <summary>Matches the old status text verbatim for a restore that could not read the
        /// chosen file (<c>"Restore failed: the file could not be read."</c>).</summary>
        internal const string RestoreFailedMessage = "Restore failed: the file could not be read.";
    }
}
