namespace Zoom_UI.MVVM.Models;

public class MessageInfo
{
    public int SenderId {  get; set; }
    public int ReceiverId {  get; set; }
    public string? Content { get; set; }


    public MessageInfo(int senderId, int receiverId, string content)
    {
        SenderId = senderId;
        ReceiverId = receiverId;
        Content = content;
    }
}
