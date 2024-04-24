using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
namespace Zoom_UI.Extensions;

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

    public static Bitmap AsBitmap(this byte[] bytes)
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
    }
}
