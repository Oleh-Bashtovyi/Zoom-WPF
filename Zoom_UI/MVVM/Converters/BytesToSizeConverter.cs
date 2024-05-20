using System.Globalization;
using System.Windows.Data;
namespace Zoom_UI.MVVM.Converters;

public class BytesToSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long size)
        {
            return FormatFileSize(size);
        }

        return value;
    }

    private string FormatFileSize(long fileSizeInBytes)
    {
        const int kb = 1024;
        const int mb = 1024 * kb;
        const int gb = 1024 * mb;


        if (fileSizeInBytes < kb)
        {
            return $"{fileSizeInBytes} B";
        }
        else if (fileSizeInBytes < mb)
        {
            double sizeInKb = (double)fileSizeInBytes / kb;
            return $"{sizeInKb:F2} KB";
        }
        else if (fileSizeInBytes < gb)
        {
            double sizeInMb = (double)fileSizeInBytes / mb;
            return $"{sizeInMb:F2} MB";
        }
        return "Error";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}