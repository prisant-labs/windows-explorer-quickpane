using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using QuickPane.Models;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #92 (Pane, Pane sections, and Recent apps pages). Covers AC-12, AC-13, AC-14. See the
    // README's "Settings inventory" for every control this page must carry over from
    // SettingsPanel.xaml.cs's "Pane" tab (left column), and the old code at commit 068301b.

    /// <summary>Pane mode and File dialogs cards (combo boxes), a Desktop dock expander (toggle in
    /// the header, auto-hide and all-desktops as child rows), and a Profile tabs expander (toggle in
    /// the header, auto-hide as a child row) (AC-12). Every setting here reads and writes the same
    /// <see cref="AppSettings"/> field, with the same values and the same immediate
    /// <c>SettingsStore.Save()</c>, as the matching handler in <c>SettingsPanel.xaml.cs</c> at
    /// commit 068301b (<c>PaneModeBlock</c>, <c>DialogModeBlock</c>, <c>DockBlock</c>,
    /// <c>ProfileTabsBlock</c>).</summary>
    internal sealed class PanePage : SettingsPage
    {
        public override string Id => "pane";
        public override string Title => "Pane";
        public override string Glyph => "";   // Dock panel (first pass, unverified - see #96)

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get
            {
                yield return new SettingsSearchEntry(Id, "paneMode", "Pane mode", "Where the pane attaches to Explorer windows.", "inside", "beside");
                yield return new SettingsSearchEntry(Id, "fileDialogs", "File dialogs", "Where the pane attaches to Open and Save dialogs.", "dialog", "open", "save");
                yield return new SettingsSearchEntry(Id, "desktopDock", "Desktop dock", "A screen-edge dock independent of the window pane.", "dock", "auto-hide", "virtual desktop");
                yield return new SettingsSearchEntry(Id, "profileTabs", "Profile tabs", "The row of profile tabs under the title.", "profile tabs", "tab row");
            }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var s = ctx.Settings.Current;
            var cards = ctx.Cards;
            var panel = new StackPanel();

            // ---- Pane mode (old: PaneModeBlock, lines 379-388) ----
            string mode = (s.Mode ?? "inside").ToLowerInvariant();
            var modeCombo = BuildCombo(
                new[]
                {
                    new KeyValuePair<string, string>("inside", "Inside Explorer window"),
                    new KeyValuePair<string, string>("beside", "Beside Explorer window"),
                    new KeyValuePair<string, string>("off", "Off (desktop dock only)"),
                },
                mode,
                v => { s.Mode = v; ctx.Settings.Save(); });
            panel.Children.Add(cards.Card("paneMode", "", "Pane mode",
                "Where the pane attaches to an Explorer window", modeCombo, "Pane mode"));

            // ---- File dialogs (old: DialogModeBlock, lines 393-412) ----
            string dialogMode = (s.DialogMode ?? "same").ToLowerInvariant();
            var dialogCombo = BuildCombo(
                new[]
                {
                    new KeyValuePair<string, string>("same", "Same as Explorer windows"),
                    new KeyValuePair<string, string>("beside", "Beside the dialog"),
                    new KeyValuePair<string, string>("off", "Off"),
                },
                dialogMode,
                v => { s.DialogMode = v; ctx.Settings.Save(); });
            panel.Children.Add(cards.Card("fileDialogs", "", "File dialogs",
                "Off also hides App Folders and \"Always start here\", which only appear in dialog panes.",
                dialogCombo, "File dialogs"));

            // ---- Desktop dock (old: DockBlock, lines 414-424) ----
            var dockToggle = cards.Toggle(s.DesktopDock, on => { s.DesktopDock = on; ctx.Settings.Save(); });
            var dockExpander = cards.Expander("desktopDock", "", "Desktop dock",
                "Pin a small dock to a screen edge", dockToggle, accessibleName: "Show desktop dock");
            dockExpander.AddChild(cards.ChildRow("Auto-hide (slide out on hover)",
                cards.Toggle(s.DesktopDockAutoHide, on => { s.DesktopDockAutoHide = on; ctx.Settings.Save(); }),
                "Auto-hide the desktop dock"));
            dockExpander.AddChild(cards.ChildRow("Show on all virtual desktops",
                cards.Toggle(s.DesktopDockAllDesktops, on => { s.DesktopDockAllDesktops = on; ctx.Settings.Save(); }),
                "Show the desktop dock on all virtual desktops"));
            panel.Children.Add(dockExpander.Root);

            // ---- Profile tabs (old: ProfileTabsBlock, lines 470-487) ----
            var tabsToggle = cards.Toggle(s.ShowProfileTabs, on => { s.ShowProfileTabs = on; ctx.Settings.Save(); });
            var tabsExpander = cards.Expander("profileTabs", "", "Profile tabs",
                "The row appears once there are two or more profiles.", tabsToggle,
                accessibleName: "Show profiles tab row");
            tabsExpander.AddChild(cards.ChildRow("Auto-hide (slide out on hover)",
                cards.Toggle(s.ProfileTabsAutoHide, on => { s.ProfileTabsAutoHide = on; ctx.Settings.Save(); }),
                "Auto-hide the profiles tab row"));
            panel.Children.Add(tabsExpander.Root);

            return panel;
        }

        // ---- combo box ------------------------------------------------------
        //
        // SettingsCards.cs is frozen for page workers and has no combo-box builder yet (#92 and #94
        // both need one). Built privately here; #94's SSH page needs the same shape for its
        // Authentication drop-down and should get its own private copy rather than this one moving,
        // per the task's file-ownership rule. If a third page needs this, it belongs in
        // SettingsCards.cs instead -- noted for the main session.

        /// <summary>A Fluent-themed combo box with one item per (value, label) pair, selecting
        /// whichever value matches <paramref name="current"/> (case-insensitive), and invoking
        /// <paramref name="onChanged"/> with the chosen value on selection. Carries no local colour:
        /// it relies entirely on the implicit ComboBox/ComboBoxItem styles merged into the window's
        /// Resources by <c>FluentThemeService</c> (AC-20).</summary>
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
