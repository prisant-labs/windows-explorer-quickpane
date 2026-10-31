using System.Threading;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using NUnit.Framework;
using QuickPane.UI.Settings;

namespace QuickPane.Tests
{
    /// <summary>The card builders take an optional accessible name, and AutomationProperties.Name
    /// throws on null. The Pane page builds its toggles unnamed and lets Card() and ChildRow() name
    /// them, so a null name must be skipped rather than set. Builds controls only; no window.</summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class SettingsCardsNamingTests
    {
        [Test]
        public void Toggle_WithoutName_DoesNotThrow()
        {
            var cards = new SettingsCards();
            Assert.DoesNotThrow(() => cards.Toggle(true, null));
        }

        [Test]
        public void ChildRow_NamesAnUnnamedToggle()
        {
            var cards = new SettingsCards();
            var toggle = cards.Toggle(false, null);
            cards.ChildRow("Auto-hide (slide out on hover)", toggle, "Auto-hide the desktop dock");
            var button = (ToggleButton)toggle.Tag;
            Assert.That(AutomationProperties.GetName(button), Is.EqualTo("Auto-hide the desktop dock"));
        }

        [Test]
        public void ChildRow_WithNoLabelOrName_DoesNotThrow()
        {
            var cards = new SettingsCards();
            Assert.DoesNotThrow(() => cards.ChildRow(null, cards.Toggle(false, null)));
        }
    }
}
