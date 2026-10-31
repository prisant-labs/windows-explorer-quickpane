using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using QuickPane.Models;

namespace QuickPane.UI.Settings.Pages
{
    // Carries over every control from SettingsPanel.xaml.cs's "Pane" tab (left column), at commit
    // 068301b.

    /// <summary>Pane mode and File dialogs cards (combo boxes), a Desktop dock expander (toggle in
    /// the header, auto-hide and all-desktops as child rows), and a Profile tabs expander (toggle in
    /// the header, auto-hide as a child row). Every setting here reads and writes the same
    /// <see cref="AppSettings"/> field, with the same values and the same immediate
    /// <c>SettingsStore.Save()</c>, as the matching handler in <c>SettingsPanel.xaml.cs</c> at
    /// commit 068301b (<c>PaneModeBlock</c>, <c>DialogModeBlock</c>, <c>DockBlock</c>,
    /// <c>ProfileTabsBlock</c>).</summary>
    internal sealed class PanePage : SettingsPage
    {
        public override string Id => "pane";
        public override string Title => "Pane";
        public override string Glyph => "";   // Segoe Fluent Icons Dock panel

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
            var modeCombo = cards.Combo(
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
            var dialogCombo = cards.Combo(
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
    }
}
