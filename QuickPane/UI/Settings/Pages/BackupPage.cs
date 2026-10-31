using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #93 (Keyboard and Backup & restore pages). Covers AC-15, AC-17. See the README's
    // "Settings inventory" for every control this page must carry over from
    // SettingsPanel.xaml.cs's "Backup" tab, and the old code at commit 068301b. The old footer's
    // version/update-check UI does NOT move here; it moves to About (#95, AC-18).

    /// <summary>Back up and Restore buttons, with the result in an info bar. A successful restore
    /// calls <see cref="SettingsPageContext.Rebuild"/>, as today (AC-17).</summary>
    internal sealed class BackupPage : SettingsPage
    {
        public override string Id => "backup";
        public override string Title => "Backup & restore";
        public override string Glyph => "";   // Cloud backup

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get { yield return new SettingsSearchEntry(Id, null, Title, "Export or import every setting as one file.", "backup", "restore", "export", "import"); }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var text = new TextBlock { Text = "Backup & restore arrives in #93.", FontSize = 14 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return text;
        }
    }
}
