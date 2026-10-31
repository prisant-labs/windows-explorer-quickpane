using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #93 (Keyboard and Backup & restore pages). Covers AC-15, AC-17. See the README's
    // "Settings inventory" for every control this page must carry over from
    // SettingsPanel.xaml.cs's "Hotkeys" tab, and the old code at commit 068301b. The existing
    // hotkey validation tests must still pass unchanged.

    /// <summary>A toggle for "Let a keyboard shortcut move focus into the pane", and a Shortcut
    /// card with a text box that validates as today and reverts to the last valid value (AC-15).</summary>
    internal sealed class KeyboardPage : SettingsPage
    {
        public override string Id => "keyboard";
        public override string Title => "Keyboard";
        public override string Glyph => "";   // Keyboard

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get { yield return new SettingsSearchEntry(Id, null, Title, "A shortcut that moves keyboard focus into the pane.", "hotkey", "shortcut", "focus"); }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var text = new TextBlock { Text = "Keyboard arrives in #93.", FontSize = 14 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return text;
        }
    }
}
