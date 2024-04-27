using System.Windows.Media.Imaging;
namespace Zoom_UI.MVVM.Models;

public class CameraFrame
{
    public int UserId { get; set; }
    public BitmapImage Image { get; set; }

    public CameraFrame(int userId, BitmapImage frame)
    {
        UserId = userId;
        Image = frame;
    }
}
