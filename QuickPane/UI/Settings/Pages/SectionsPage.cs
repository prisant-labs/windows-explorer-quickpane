using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #92 (Pane, Pane sections, and Recent apps pages). Covers AC-12, AC-13, AC-14. See the
    // README's "Settings inventory" for every control this page must carry over from
    // SettingsPanel.xaml.cs's "Pane" tab (right column), and the old code at commit 068301b.

    /// <summary>One card per pane section, each with a visibility toggle and Move up / Move down
    /// buttons in place of today's drag-reorder (AC-13).</summary>
    internal sealed class SectionsPage : SettingsPage
    {
        public override string Id => "sections";
        public override string Title => "Pane sections";
        public override string Glyph => "";   // Bulleted list

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get { yield return new SettingsSearchEntry(Id, null, Title, "Which sections the pane shows, and in what order.", "section", "order", "reorder"); }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var text = new TextBlock { Text = "Pane sections arrives in #92.", FontSize = 14 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return text;
        }
    }
}
