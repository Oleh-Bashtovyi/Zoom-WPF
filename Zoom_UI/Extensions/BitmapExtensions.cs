﻿using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace Zoom_UI.Extensions;

static class BitmapExtensions
{
    public static BitmapImage ToBitmapImage(this Bitmap bitmap)
    {
        BitmapImage bi = new BitmapImage();
        bi.BeginInit();
        MemoryStream ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Bmp);
        ms.Seek(0, SeekOrigin.Begin);
        bi.StreamSource = ms;
        bi.EndInit();
        return bi;
    }

    public static byte[] ToByteArray(this Bitmap bitmap)
    {
        MemoryStream ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Bmp);
        var result = ms.ToArray();
        ms.Dispose();
        return result;
    }
}
