using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI.MVVM.Models;

#pragma warning disable CS8618

public class UserModel
{
    public int UID { get; set; }
    public string Username { get; set; }



    public UserModel() { }
    public UserModel(int UID, string Username)
    {
        this.UID = UID;
        this.Username = Username;
    }


    public UserViewModel AsViewModel()
    {
        return new UserViewModel(Username, UID);
    }
}

#pragma warning restore CS8618
