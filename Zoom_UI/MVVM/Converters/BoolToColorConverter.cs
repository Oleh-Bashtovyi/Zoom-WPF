using System.Globalization;
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
            return Brushes.Green;
        }
        else
        {
            return Brushes.LightYellow;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
