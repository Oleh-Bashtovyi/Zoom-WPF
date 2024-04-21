using System.Globalization;
using System.Windows.Media.Imaging;

namespace Zoom_UI.MVVM.Converters;

public class BoolToCameraImageConverter
{
    private static BitmapImage cam_on = new(new("pack://application:,,,/Zoom_UI;Assets/cam_on.png"));
    private static BitmapImage cam_off = new(new("pack://application:,,,/Zoom_UI;Assets/cam_off.png"));

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
