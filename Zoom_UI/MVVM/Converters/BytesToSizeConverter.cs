using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace Zoom_UI.MVVM.Converters;

public class BytesToSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte[] bytes)
        {
            return FormatFileSize(bytes.Length);
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }



    private string FormatFileSize(int fileSizeInBytes)
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







    private IReadOnlyDictionary<string, string> _fileExtensionNameToName = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()
    {
        {".png", "image, png" },
        {".html", "html" },
        {".jpg", "image, jpg" },
        {".pptx", "presentation" },
        {".docx", "document" },
        {".xls", "excel table" },
        {".txt", "text" },
        {".cs", "C# file" }
    });
    public List<string> GetSupportedExtensions()
    {
        return _fileExtensionNameToName.Keys.ToList();
    }
}







