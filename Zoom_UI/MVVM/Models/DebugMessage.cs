namespace Zoom_UI.MVVM.Models;

public class DebugMessage
{
    public string Content { get; set; }
    public DateTime Time { get; set; }
    public DebugMessage(string content)
    {
        Content = content;
        Time = DateTime.Now;
    }
}
