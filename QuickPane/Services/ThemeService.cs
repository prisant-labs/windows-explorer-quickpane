using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace QuickPane.Services
{
    /// <summary>
    /// Owns the active theme. Reads AppsUseLightTheme and the accent color from the registry, loads the
    /// matching ResourceDictionary, and overrides the accent brushes from the live system accent.
    /// SystemEvents.UserPreferenceChanged fires when the user flips dark/light or changes accent, so
    /// the sidebar restyles with no restart.
    /// </summary>
    public sealed class ThemeService : IDisposable
    {
        public event Action ThemeChanged;

        public bool IsDark { get; private set; }
        public Color Accent { get; private set; } = Color.FromArgb(0xFF, 0x00, 0x78, 0xD4);

        private const string PersonalizeKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        // The accent is not under Personalize, which holds only the mode and transparency switches.
        // Reading it there always missed, so the pane kept its default blue (#20). DWM holds the
        // accent itself; Explorer\Accent keeps a copy for the Start menu.
        private const string DwmKey = @"SOFTWARE\Microsoft\Windows\DWM";
        private const string ExplorerAccentKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Accent";
        private ResourceDictionary _current;

        public void Start()
        {
            ReadRegistry();
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        public void Apply()
        {
            var app = Application.Current;
            if (app == null) return;

            var uri = IsDark
                ? new Uri("/QuickPane;component/Themes/Theme.Dark.xaml", UriKind.Relative)
                : new Uri("/QuickPane;component/Themes/Theme.Light.xaml", UriKind.Relative);

            ResourceDictionary dict;
            try
            {
                dict = (ResourceDictionary)Application.LoadComponent(uri);
            }
            catch (Exception ex)
            {
                Log.Error("theme dictionary load failed", ex);
                return;
            }

            // Override accent brushes from the live system accent so it always matches Windows.
            var accentBrush = new SolidColorBrush(Accent); accentBrush.Freeze();
            var accentSoft = new SolidColorBrush(WithAlpha(Accent, 0x33)); accentSoft.Freeze();
            var accentHover = new SolidColorBrush(WithAlpha(Accent, 0x26)); accentHover.Freeze();
            dict["AccentBrush"] = accentBrush;
            dict["ItemActiveBackground"] = accentSoft;
            dict["ItemHoverBackground"] = accentHover;

            var merged = app.Resources.MergedDictionaries;
            if (_current != null) merged.Remove(_current);
            merged.Add(dict);
            _current = dict;

            ThemeChanged?.Invoke();
        }

        /// <summary>Windows hosted as HwndSource roots fall back to Application resources, so this
        /// is a no-op kept for call-site clarity. It exists so callers do not have to know that.</summary>
        public void AttachWindow(Window window) { }

        private void ReadRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey))
                {
                    var light = key?.GetValue("AppsUseLightTheme");
                    IsDark = light is int && (int)light == 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error("theme registry read failed", ex);
                IsDark = false;
            }

            var accent = ParseAccent(ReadValue(DwmKey, "AccentColor"))
                ?? ParseAccent(ReadValue(ExplorerAccentKey, "AccentColorMenu"));
            if (accent.HasValue) Accent = accent.Value;
        }

        private static object ReadValue(string keyPath, string name)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                    return key?.GetValue(name);
            }
            catch (Exception ex)
            {
                Log.Error("theme registry read failed: " + name, ex);
                return null;
            }
        }

        /// <summary>The accent from a registry value, or null when the value is missing or is not a
        /// DWORD. The registry hands a DWORD back as a signed int.</summary>
        internal static Color? ParseAccent(object value)
        {
            if (!(value is int)) return null;
            return AbgrToColor(unchecked((uint)(int)value));
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General &&
                e.Category != UserPreferenceCategory.Color &&
                e.Category != UserPreferenceCategory.VisualStyle)
                return;

            var app = Application.Current;
            if (app == null) return;
            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                ReadRegistry();
                Apply();
            }));
        }

        // Registry AccentColor is 0xAABBGGRR; WPF wants ARGB.
        internal static Color AbgrToColor(uint v)
        {
            byte a = (byte)((v >> 24) & 0xFF);
            byte b = (byte)((v >> 16) & 0xFF);
            byte g = (byte)((v >> 8) & 0xFF);
            byte r = (byte)(v & 0xFF);
            if (a == 0) a = 0xFF;
            return Color.FromArgb(a, r, g, b);
        }

        private static Color WithAlpha(Color c, byte a)
        {
            return Color.FromArgb(a, c.R, c.G, c.B);
        }

        public void Dispose()
        {
            try { SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged; } catch { }
        }
    }
}
