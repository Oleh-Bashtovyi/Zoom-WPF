using System.Windows.Media.Imaging;
namespace Zoom_UI.MVVM.Models;

public class ImageFrame
{
    public int UserId { get; set; }
    public BitmapImage Image { get; set; }

    public ImageFrame(int userId, BitmapImage frame)
    {
        UserId = userId;
        Image = frame;
    }
}
