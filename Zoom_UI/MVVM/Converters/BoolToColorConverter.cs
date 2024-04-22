﻿using System.Globalization;
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
            return Brushes.LightYellow;
        }
        else
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3F23"));
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
