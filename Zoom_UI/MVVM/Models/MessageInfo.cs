namespace Zoom_UI.MVVM.Models;

public class MessageInfo
{
    public int SenderId {  get; set; }
    public int ReceiverId {  get; set; }
    public object? Content { get; set; }


    public MessageInfo(int senderId, int receiverId, object content)
    {
        SenderId = senderId;
        ReceiverId = receiverId;
        Content = content;
    }
}
