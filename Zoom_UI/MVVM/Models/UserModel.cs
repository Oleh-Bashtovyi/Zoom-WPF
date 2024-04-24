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


    public override bool Equals(object? obj)
    {
        if (obj is UserViewModel vm)
        {
            return UID == vm.UID;
        }
        else if (obj is UserModel model)
        {
            return UID == model.UID;
        }
        return false;
    }

    public UserViewModel AsViewModel()
    {
        return new UserViewModel(Username, UID);
    }
}

#pragma warning restore CS8618
