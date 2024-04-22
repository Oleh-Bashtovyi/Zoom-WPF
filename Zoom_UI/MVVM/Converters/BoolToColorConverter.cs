using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Zoom_UI.MVVM.Converters;

internal class BoolToColorConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = (bool)value;

        if (boolValue)
        {
            //(Brush)Application.Current.FindResource("FromBrush");
            //return Brushes.LightYellow;
            //return (Brush)Application.Current.FindResource("OnButton");
            return Application.Current.Resources["OnButton"] as SolidColorBrush;
        }
        else
        {
            //return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3F23"));
            //return (Brush)Application.Current.FindResource("OffButton");
            return Application.Current.Resources["OffButton"] as SolidColorBrush;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
