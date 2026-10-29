using System.IO;
using NUnit.Framework;
using QuickPane.Services;

namespace QuickPane.Tests
{
    /// <summary>
    /// Covers TriggersRescan, the filter that stops writes the groups scan never reads from setting off
    /// a full rescan (#49). Paths are invented; no folder is watched.
    /// </summary>
    [TestFixture]
    public class GroupStoreTests
    {
        private const string Tab = @"C:\Demo\Groups\01_Work\01_Projects";

        [Test]
        public void TriggersRescan_PinEditedInPlace_Rescans()
        {
            // A shortcut's target changed in its Properties dialog raises only a write-time change.
            Assert.That(GroupStore.TriggersRescan(WatcherChangeTypes.Changed, Tab + @"\001_Reports.lnk"), Is.True);
            Assert.That(GroupStore.TriggersRescan(WatcherChangeTypes.Changed, Tab + @"\002_Archive.LNK"), Is.True);
        }

        [Test]
        public void TriggersRescan_FolderWriteTime_Ignored()
        {
            // Fires whenever a file inside the tab is added or removed, which raises its own event.
            Assert.That(GroupStore.TriggersRescan(WatcherChangeTypes.Changed, Tab), Is.False);
        }

        [Test]
        public void TriggersRescan_OtherFileWritten_Ignored()
        {
            Assert.That(GroupStore.TriggersRescan(WatcherChangeTypes.Changed, Tab + @"\desktop.ini"), Is.False);
            Assert.That(GroupStore.TriggersRescan(WatcherChangeTypes.Changed, Tab + @"\Thumbs.db"), Is.False);
        }

        [Test]
        public void TriggersRescan_CreateDeleteRename_AlwaysRescan()
        {
            Assert.That(GroupStore.TriggersRescan(WatcherChangeTypes.Created, Tab + @"\003_New.lnk"), Is.True);
            Assert.That(GroupStore.TriggersRescan(WatcherChangeTypes.Deleted, Tab), Is.True);
            Assert.That(GroupStore.TriggersRescan(WatcherChangeTypes.Renamed, Tab + @"\desktop.ini"), Is.True);
        }

        [Test]
        public void TriggersRescan_NullPath_IgnoredForChanges()
        {
            Assert.That(GroupStore.TriggersRescan(WatcherChangeTypes.Changed, null), Is.False);
        }
    }
}
