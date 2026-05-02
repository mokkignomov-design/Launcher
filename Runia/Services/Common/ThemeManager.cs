using System.Windows;

namespace Runia.Services.Common
{
    public static class ThemeManager
    {
        private const string THEME_FOLDER = "assets/Themes/";
        public static void ApplyTheme(string themeName)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var oldTheme = dictionaries.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.EndsWith("Theme.xaml"));
            string newThemePath = $"pack://application:,,,/{THEME_FOLDER}{themeName}Theme.xaml";

            try
            {
                var newTheme = new ResourceDictionary
                {
                    Source = new Uri(newThemePath, UriKind.Absolute)
                };

                if (oldTheme != null)
                {
                    int index = dictionaries.IndexOf(oldTheme);
                    dictionaries[index] = newTheme;
                }
                else
                {
                    dictionaries.Insert(0, newTheme);
                }
                Properties.Settings.Default.LastTheme = themeName;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при смене темы: {ex.Message}");
            }
        }
        public static void Initialize()
        {
            string lastTheme = Properties.Settings.Default.LastTheme;
            if (string.IsNullOrEmpty(lastTheme)) lastTheme = "Dark";
            ApplyTheme(lastTheme);
        }
    }
}