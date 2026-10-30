using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #92 (Pane, Pane sections, and Recent apps pages). Covers AC-12, AC-13, AC-14. See the
    // README's "Settings inventory" for every control this page must carry over from
    // SettingsPanel.xaml.cs's "Pane" tab (the Recent Apps block), and the old code at commit
    // 068301b. The 2026-10-30 prototype gives Recent apps its own page instead of sharing Pane's
    // crowded left column.

    /// <summary>A single card with a combo box for the Recent Apps source, and the explanatory note
    /// the old column showed beside the radios, folded into the card's description (AC-14).</summary>
    internal sealed class RecentAppsPage : SettingsPage
    {
        public override string Id => "recent";
        public override string Title => "Recent apps";
        public override string Glyph => "";   // History

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get { yield return new SettingsSearchEntry(Id, null, Title, "Which apps' recent folders the pane shows.", "recent", "history", "jump list"); }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var text = new TextBlock { Text = "Recent apps arrives in #92.", FontSize = 14 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return text;
        }
    }
}
