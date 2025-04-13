using System.Windows.Media.Imaging;
namespace Zoom_UI.MVVM.Models.Frames;

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


public class UserFrame
{
    public int UserId { get; set; }
    public int Position {  get; set; }
    public byte[] Data { get; set; }
    public UserFrame(int userId, int position, byte[] data)
    {
        UserId=userId;
        Data = data;
    }
}