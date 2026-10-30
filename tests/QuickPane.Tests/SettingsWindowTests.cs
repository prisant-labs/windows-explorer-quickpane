using System.Windows;
using NUnit.Framework;
using QuickPane.UI.Settings;

namespace QuickPane.Tests
{
    /// <summary>The one layout decision in SettingsWindow.cs that can be tested with no window at
    /// all: how much margin compensates for the resize-frame overhang WindowChrome leaves when
    /// maximized. See AC-27 ("every testable decision lives in an internal static method").</summary>
    [TestFixture]
    public class SettingsWindowTests
    {
        [Test]
        public void RootMarginFor_Maximized_UsesResizeBorderThickness()
        {
            var border = new Thickness(7);
            Assert.That(SettingsWindow.RootMarginFor(WindowState.Maximized, border), Is.EqualTo(border));
        }

        [TestCase(WindowState.Normal)]
        [TestCase(WindowState.Minimized)]
        public void RootMarginFor_NotMaximized_IsZero(WindowState state)
        {
            var border = new Thickness(7);
            Assert.That(SettingsWindow.RootMarginFor(state, border), Is.EqualTo(new Thickness(0)));
        }
    }
}
