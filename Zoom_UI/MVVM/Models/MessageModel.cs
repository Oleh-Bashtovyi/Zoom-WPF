namespace Zoom_UI.MVVM.Models;

public class MessageModel
{
    public string From {  get; set; }
    public string To { get; set; }
    public DateTime When { get; set; }
    public object Content { get; set; }


    public MessageModel() { }

    public MessageModel(string from, string to, DateTime when, object content)
    {
        From = from;
        To = to;
        When = when;
        Content = content;
    }
}
