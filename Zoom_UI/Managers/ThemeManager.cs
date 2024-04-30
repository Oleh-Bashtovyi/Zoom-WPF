using System.Windows;

namespace Zoom_UI.Managers;

public class ThemeManager
{
    private int CurrentThemeIndex { get; set; }
    public string CurrentTheme => _themes[CurrentThemeIndex];

    private List<string> _themes = [
        "Light",
        "Dark"
        ];

    public event Action<string>? OnThemeChanged;


    public ThemeManager()
    {
        CurrentThemeIndex = 0;
    }

    public void NextTheme()
    {
        var previous = CurrentThemeIndex;
        CurrentThemeIndex += 1;
        CurrentThemeIndex %= _themes.Count;

        var newThemeDict = new ResourceDictionary()
        {
            Source = new Uri($"Themes/{_themes[CurrentThemeIndex]}Theme.xaml", UriKind.Relative)
        };

        var oldTheme = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString == $"Themes/{_themes[previous]}Theme.xaml");

        if (oldTheme != null && newThemeDict != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(oldTheme);
            Application.Current.Resources.MergedDictionaries.Add(newThemeDict);
            OnThemeChanged?.Invoke(CurrentTheme);
        }
    }



    /*    private void ReplaceTheme(string newTheme)
        {
            var newThemeDict = new ResourceDictionary()
            {
                Source = new Uri($"Themes/{newTheme}.xaml", UriKind.Relative)
            };


            ResourceDictionary oldTheme = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source.OriginalString == "Themes/LightTheme.xaml");

            if (oldTheme != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(oldTheme);
            }
            Application.Current.Resources.MergedDictionaries.Add(newThemeDict);
        }*/
}
