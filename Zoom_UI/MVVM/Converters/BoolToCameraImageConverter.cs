using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
namespace Zoom_UI.MVVM.Converters;


[ValueConversion(typeof(bool), typeof(BitmapImage))]
public class BoolToCameraImageConverter : IValueConverter
{
/*    private static BitmapImage cam_on = new(new("pack://siteoforigin:,,,/Zoom_UI;Assets/cam_on.png", UriKind.Absolute));
    private static BitmapImage cam_off = new(new("pack://siteoforigin:,,,/Zoom_UI;Assets/cam_off.png", UriKind.Absolute));*/
    private static BitmapImage cam_on = new(new("pack://siteoforigin:,,,/Assets/cam_on.png", UriKind.Absolute));
    private static BitmapImage cam_off = new(new("pack://siteoforigin:,,,/Assets/cam_off.png", UriKind.Absolute));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = (bool)value;

        if (boolValue)
        {
            return cam_on;
        }
        else
        {
            return cam_off;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
