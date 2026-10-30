using System;
using NUnit.Framework;
using QuickPane.Explorer;

namespace QuickPane.Tests
{
    /// <summary>
    /// Covers DragYieldActive, the cap that hands a dragged window back to normal layout when the
    /// end-of-drag event is lost. No window or hook is needed.
    /// </summary>
    [TestFixture]
    public class ExplorerWatcherTests
    {
        [Test]
        public void DragYieldActive_WithinCap_IsTrue()
        {
            Assert.That(ExplorerWatcher.DragYieldActive(1000, 1000), Is.True);
            Assert.That(ExplorerWatcher.DragYieldActive(1000, 1000 + ExplorerWatcher.DragYieldCapMs - 1), Is.True);
        }

        [Test]
        public void DragYieldActive_AtOrPastCap_IsFalse()
        {
            Assert.That(ExplorerWatcher.DragYieldActive(1000, 1000 + ExplorerWatcher.DragYieldCapMs), Is.False);
            Assert.That(ExplorerWatcher.DragYieldActive(1000, 1000 + 10 * ExplorerWatcher.DragYieldCapMs), Is.False);
        }

        [Test]
        public void DragYieldActive_AcrossTickCountWrap_StillCountsElapsedTime()
        {
            // Environment.TickCount runs from int.MaxValue to int.MinValue after 24.9 days.
            int start = int.MaxValue - 100;
            Assert.That(ExplorerWatcher.DragYieldActive(start, unchecked(start + 500)), Is.True);
            Assert.That(ExplorerWatcher.DragYieldActive(start, unchecked(start + ExplorerWatcher.DragYieldCapMs + 1)), Is.False);
        }

        [Test]
        public void DragYieldActive_ClockBeforeStart_IsFalse()
        {
            // A start time in the future means the state is stale; never yield on it.
            Assert.That(ExplorerWatcher.DragYieldActive(5000, 4000), Is.False);
        }
    }
}
