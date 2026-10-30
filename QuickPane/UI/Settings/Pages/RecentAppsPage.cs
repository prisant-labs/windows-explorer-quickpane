using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using QuickPane.Models;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #92 (Pane, Pane sections, and Recent apps pages). Covers AC-12, AC-13, AC-14. See the
    // README's "Settings inventory" for every control this page must carry over from
    // SettingsPanel.xaml.cs's "Pane" tab (the Recent Apps block), and the old code at commit
    // 068301b. The 2026-10-30 prototype gives Recent apps its own page instead of sharing Pane's
    // crowded left column.

    /// <summary>A single card with a combo box for the Recent Apps source, and the explanatory note
    /// the old column showed beside the radios, folded into the card's description (AC-14). Reads
    /// and writes <see cref="AppSettings.RecentAppsSource"/> with the same values ("both", "open",
    /// "dialogs") and the same immediate <c>SettingsStore.Save()</c> as the old
    /// <c>RecentAppsBlock</c>/<c>SourceRadio</c> at commit 068301b.</summary>
    internal sealed class RecentAppsPage : SettingsPage
    {
        public override string Id => "recent";
        public override string Title => "Recent apps";
        public override string Glyph => "";   // History (first pass, unverified - see #96)

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get { yield return new SettingsSearchEntry(Id, "recentAppsSource", Title, "Which apps' recent folders the pane shows.", "recent", "history", "jump list"); }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var s = ctx.Settings.Current;
            var cards = ctx.Cards;

            string current = (s.RecentAppsSource ?? "both").ToLowerInvariant();
            var combo = BuildCombo(
                new[]
                {
                    new KeyValuePair<string, string>("both", "Apps I open and apps I save from"),
                    new KeyValuePair<string, string>("open", "Only apps I open"),
                    new KeyValuePair<string, string>("dialogs", "Only apps whose Save and Open dialogs I use"),
                },
                current,
                v => { s.RecentAppsSource = v; ctx.Settings.Save(); });

            var panel = new StackPanel();
            panel.Children.Add(cards.Card("recentAppsSource", "", "Which apps to list",
                "A row opens its app. The folder that app's dialog was last sent to is on the row's right-click menu.",
                combo, "Recent apps source"));
            return panel;
        }

        // Private copy of the same combo-box builder PanePage.cs uses. SettingsCards.cs is frozen
        // for page workers and has no combo-box builder yet; see the note in PanePage.cs.
        private static ComboBox BuildCombo(IEnumerable<KeyValuePair<string, string>> options, string current, System.Action<string> onChanged)
        {
            var box = new ComboBox { MinWidth = 220 };
            int selected = -1;
            int i = 0;
            foreach (var opt in options)
            {
                box.Items.Add(new ComboBoxItem { Content = opt.Value, Tag = opt.Key });
                if (string.Equals(opt.Key, current, System.StringComparison.OrdinalIgnoreCase)) selected = i;
                i++;
            }
            box.SelectedIndex = selected >= 0 ? selected : 0;
            box.SelectionChanged += (s, e) =>
            {
                var item = box.SelectedItem as ComboBoxItem;
                if (item != null) onChanged((string)item.Tag);
            };
            return box;
        }
    }
}
