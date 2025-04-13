using System.IO;
using System.Windows.Media.Imaging;
namespace Zoom_UI.Extensions;

public static class BitmapImageExtensionss
{
    public static MemoryStream AsMemoryStream(this BitmapImage image)
    {
        //JPEG TAKES OUT ALL ALPHA CHANEL!!!
        return image.AsMemoryStream(new JpegBitmapEncoder());
    }

    public static MemoryStream AsMemoryStream(this BitmapImage image, BitmapEncoder encoder)
    {
        var ms = new MemoryStream();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(ms);
        return ms;
    }
}
