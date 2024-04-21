namespace Zoom_UI.MVVM.Models;

public class User
{
    public string UserName { get; set; }
    public string UID { get; set; }
    public char GetLetter => UserName[0];

    public User() { }

    public User(string username, string uid)
    {
        UserName = username;
        UID = uid;
    }
}
