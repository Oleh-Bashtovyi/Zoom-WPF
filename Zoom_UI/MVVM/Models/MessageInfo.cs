namespace Zoom_UI.MVVM.Models;

public class MessageInfo
{
    public string Sender {  get; set; }
    public string Receiver {  get; set; }
    public object? Content { get; set; }


    public MessageInfo(string sender, string receiver, object content)
    {
        Sender = sender;
        Receiver = receiver;
        Content = content;
    }
}
