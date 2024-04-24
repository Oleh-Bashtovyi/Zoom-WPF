using System.Collections;
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







public static class ByteExtensions
{
    public static List<byte[]> AsClusters(this byte[] bytes, int clusterSize = 4096)
    {
        var byteArrays = new List<byte[]>();
        var totalLength = bytes.Length;
        var offset = 0;

        while (totalLength > 0)
        {
            var length = Math.Min(totalLength, clusterSize);
            var chunk = new byte[length];
            Array.Copy(bytes, offset, chunk, 0, length);
            byteArrays.Add(chunk);

            offset += length;
            totalLength -= length;
        }
        return byteArrays;
    }


    public static Bitmap ToBitmap(this byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        return new Bitmap(ms);
    }


    public static BitmapImage AsBitmapImage(this byte[] bytes)
    {
        using (var stream = new MemoryStream(bytes))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze(); // optional
            return bitmap;
        }

        /*        BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                MemoryStream ms = new MemoryStream(bytes);
                ms.Seek(0, SeekOrigin.Begin);
                bi.StreamSource = ms;
                bi.EndInit();
                return bi;*/
    }
}








public static class BitmapImageExtensions
{

    public static MemoryStream AsMemoryStream(this BitmapImage image)
    {
        var ms = new MemoryStream();
        var encoder = new JpegBitmapEncoder();
        //var encoder = new BmpBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(ms);
        return ms;
    }


    public static byte[] AsByteArray(this BitmapImage image)
    {
        using (var ms = image.AsMemoryStream())
        {
            return ms.ToArray();
        }
    }
}

