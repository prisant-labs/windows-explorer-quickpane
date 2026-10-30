using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #94 (SSH drives page). Covers AC-16. See the README's "Settings inventory" for every
    // control this page must carry over from SettingsPanel.xaml.cs's "Connections" tab, and the
    // old code at commit 068301b. The 3.11 plan's risk table flags SshfsService.IsInstalled() as
    // today's one disk check on the UI thread (SettingsPanel.xaml.cs line 671): this page must move
    // it onto WorkQueue and show the setup cards once it answers, per AC-23.

    /// <summary>An info bar plus setup cards (WinFsp, SSHFS-Win, winget commands) when either
    /// component is missing, one expandable card per connection with every field from the
    /// inventory, and an "Add a connection" card (AC-16).</summary>
    internal sealed class SshPage : SettingsPage
    {
        public override string Id => "ssh";
        public override string Title => "SSH drives";
        public override string Glyph => "";   // Globe

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get { yield return new SettingsSearchEntry(Id, null, Title, "Mount a remote folder over SSH as a drive letter.", "sshfs", "winfsp", "mount", "connection"); }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var text = new TextBlock { Text = "SSH drives arrives in #94.", FontSize = 14 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return text;
        }
    }
}
