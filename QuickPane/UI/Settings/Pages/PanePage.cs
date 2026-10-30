using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #92 (Pane, Pane sections, and Recent apps pages). Covers AC-12, AC-13, AC-14. See the
    // README's "Settings inventory" for every control this page must carry over from
    // SettingsPanel.xaml.cs's "Pane" tab (left column), and the old code at commit 068301b.

    /// <summary>Pane mode and File dialogs cards (combo boxes), a Desktop dock expander (toggle in
    /// the header, auto-hide and all-desktops as child rows), and a Profile tabs expander (toggle in
    /// the header, auto-hide as a child row) (AC-12).</summary>
    internal sealed class PanePage : SettingsPage
    {
        public override string Id => "pane";
        public override string Title => "Pane";
        public override string Glyph => "";   // Dock panel

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get { yield return new SettingsSearchEntry(Id, null, Title, "How and where the pane appears.", "inside", "beside", "dock", "profile tabs", "dialog"); }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var text = new TextBlock { Text = "Pane arrives in #92.", FontSize = 14 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return text;
        }
    }
}
