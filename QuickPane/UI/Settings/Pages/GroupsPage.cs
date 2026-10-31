using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #91 (Groups & profiles page and profile sub-pages, with Move to profile). Covers
    // AC-9, AC-10, AC-11. See the README's "Settings inventory" for every control this page must
    // carry over from SettingsPanel.xaml.cs's "Groups" tab, and the old code at commit 068301b.

    /// <summary>Lists every profile as a card, with an "Add a profile" card (AC-9). Clicking a
    /// profile opens its own sub-page through <see cref="SettingsPageContext.OpenSubPage"/>: Rename,
    /// Activate, Remove, the group list (Move up/down, Move to profile, Delete, Add group), and the
    /// profile's own groups-folder, recents-count, and sidebar-width cards (AC-10, AC-11).</summary>
    internal sealed class GroupsPage : SettingsPage
    {
        public override string Id => "groups";
        public override string Title => "Groups & profiles";
        public override string Glyph => "";   // People

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get { yield return new SettingsSearchEntry(Id, null, Title, "Profiles, groups, and pinned folders.", "profile", "group", "pin"); }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var text = new TextBlock { Text = "Groups & profiles arrives in #91.", FontSize = 14 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return text;
        }
    }
}
