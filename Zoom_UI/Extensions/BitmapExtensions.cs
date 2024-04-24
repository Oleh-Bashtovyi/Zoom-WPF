using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
namespace Zoom_UI.Extensions;

static class BitmapExtensions
{
    /// <summary>
    /// Returns memory stream of bitmap. Uses Jpeg image format
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    public static MemoryStream AsMemoryStream(this Bitmap bitmap)
    {
        return bitmap.AsMemoryStream(ImageFormat.Jpeg);
    }

    public static MemoryStream AsMemoryStream(this Bitmap bitmap, ImageFormat format)
    {
        var ms = new MemoryStream();
        bitmap.Save(ms, format);
        return ms;
    }

    public static byte[] AsByteArray(this Bitmap bitmap)
    {
        using var ms = bitmap.AsMemoryStream();
        var result = ms.ToArray();
        return result;
    }

    public static BitmapImage AsBitmapImage(this Bitmap bitmap)
    {
        var ms = bitmap.AsMemoryStream();
        var bi = new BitmapImage();
        bi.BeginInit();
        ms.Seek(0, SeekOrigin.Begin);
        bi.StreamSource = ms;
        bi.EndInit();
        return bi;
    }
}