using System.Windows.Media;
using NUnit.Framework;
using QuickPane.Services;

namespace QuickPane.Tests
{
    /// <summary>Pins how the Windows accent is read from the registry. Windows stores it as a DWORD in
    /// 0xAABBGGRR order, and the registry hands a DWORD back as a signed int.</summary>
    [TestFixture]
    public class ThemeServiceTests
    {
        [Test]
        public void ParseAccent_DefaultBlue()
        {
            Assert.That(ThemeService.ParseAccent(unchecked((int)0xFFD47800)), Is.EqualTo(Color.FromArgb(0xFF, 0x00, 0x78, 0xD4)));
        }

        [Test]
        public void ParseAccent_SwapsRedAndBlue()
        {
            // An orange accent: red F7, green 63, blue 0C.
            Assert.That(ThemeService.ParseAccent(unchecked((int)0xFF0C63F7)), Is.EqualTo(Color.FromArgb(0xFF, 0xF7, 0x63, 0x0C)));
        }

        [Test]
        public void ParseAccent_ZeroAlpha_IsOpaque()
        {
            Assert.That(ThemeService.ParseAccent(0x0012A05B), Is.EqualTo(Color.FromArgb(0xFF, 0x5B, 0xA0, 0x12)));
        }

        [Test]
        public void ParseAccent_MissingValue_IsNull()
        {
            Assert.That(ThemeService.ParseAccent(null), Is.Null);
        }

        [Test]
        public void ParseAccent_NotADword_IsNull()
        {
            Assert.That(ThemeService.ParseAccent(new byte[] { 0x00, 0x78, 0xD4, 0x00 }), Is.Null);
            Assert.That(ThemeService.ParseAccent("#0078D4"), Is.Null);
        }
    }
}
