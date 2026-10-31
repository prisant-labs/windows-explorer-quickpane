using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QuickPane.UI.Settings.Pages
{
    // Owner: #95 (About page, with the update check moved from the footer). Covers AC-18. See the
    // README's "Settings inventory" for the footer content this page must carry over from
    // SettingsPanel.xaml.cs (line 1677-1755), and the old code at commit 068301b. AC-18 changes the
    // update check from running every time Settings opens to running only when this page opens or
    // its button is pressed -- keep that change; do not restore the old timing.
    //
    // This stub already declares the search entries AC-6 requires ("update" and "version" must
    // find About) because the search index this shell builds has to answer those queries before
    // #95 adds the real page; see SettingsSearchIndexTests for the test that pins this down.

    /// <summary>App header, an Updates card (status, "Check for updates", Download when an update
    /// exists), a Links card (source, release notes, report an issue), a Licence card, and a
    /// Third-party notices card (AC-18). Pinned to the bottom of the navigation pane (AC-4).</summary>
    internal sealed class AboutPage : SettingsPage
    {
        public override string Id => "about";
        public override string Title => "About";
        public override string Glyph => "";   // Info

        public override IEnumerable<SettingsSearchEntry> SearchEntries
        {
            get
            {
                yield return new SettingsSearchEntry(Id, "updates", "Check for updates",
                    "See the installed version and check for a newer release.", "update", "version", "upgrade");
                yield return new SettingsSearchEntry(Id, "about-app", "App version and licence",
                    "The installed version, and the MIT licence QuickPane is built on.", "version", "licence", "license");
                yield return new SettingsSearchEntry(Id, "links", "Source code, release notes, report an issue",
                    "Links to the repository, the changelog, and the issue tracker.", "github", "changelog", "issue");
            }
        }

        public override UIElement Build(SettingsPageContext ctx)
        {
            var text = new TextBlock { Text = "About arrives in #95.", FontSize = 14 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return text;
        }
    }
}
