using System.Globalization;
using System.Windows;
using System.Windows.Data;
namespace Zoom_UI.MVVM.Converters;

internal class BoolToVisibilityConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = (bool)value;

        if (boolValue)
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Hidden;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
