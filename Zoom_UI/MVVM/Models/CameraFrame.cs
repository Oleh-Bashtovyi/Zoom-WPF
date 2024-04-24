using System.Windows.Media.Imaging;
namespace Zoom_UI.MVVM.Models;

internal class CameraFrame
{
    internal int UserId { get; set; }
    internal BitmapImage Frame { get; set; }

    internal CameraFrame(int userId, BitmapImage frame)
    {
        UserId = userId;
        Frame = frame;
    }
}
