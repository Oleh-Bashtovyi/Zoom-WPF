using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Zoom_UI.MVVM.Converters;

public class BoolToMicrophoneImageConverter : IValueConverter
{
    /*    private static BitmapImage mic_on = new(new("pack://application:,,,/Zoom_UI;Assets/mic_on.png"));
        private static BitmapImage mic_off = new(new("pack://application:,,,/Zoom_UI;Assets/mic_off.png"));*/

    /*    private static BitmapImage mic_on = new(new("pack://application:,,,/Zoom_UI;Assets/mic_on.png", UriKind.Absolute));
        private static BitmapImage mic_off = new(new("pack://application:,,,/Zoom_UI;Assets/mic_off.png", UriKind.Absolute));*/

    /*    private static BitmapImage mic_on = new(new("Assets/mic_on.png", UriKind.Relative));
        private static BitmapImage mic_off = new(new("Assets/mic_off.png", UriKind.Relative));*/

    private static BitmapImage mic_on = new (new ("pack://siteoforigin:,,,/Assets/mic_on.png", UriKind.Absolute));
    private static BitmapImage mic_off = new (new  ("pack://siteoforigin:,,,/Assets/mic_off.png", UriKind.Absolute));
    //E:\work\course_2\semester_2\ookp\Lab_7\Zoom\Zoom_UI\Assets\mic_on.png
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = (bool)value;

        if (boolValue)
        {
            return mic_on;
        }
        else
        {
           return mic_off;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
