using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Zoom_UI.MVVM.Converters;

[ValueConversion(typeof(string), typeof(BitmapImage))]
internal class ThemeToImageConverter : IValueConverter
{

    private static BitmapImage light = new(new("pack://siteoforigin:,,,/Assets/day.png", UriKind.Absolute));
    private static BitmapImage dark = new(new("pack://siteoforigin:,,,/Assets/night.png", UriKind.Absolute));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
         if(value is string str)
         {
             if(str == "dark")
            {
                return dark;
            }
             else
            {
                return light;
            }
         }

        return light;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
