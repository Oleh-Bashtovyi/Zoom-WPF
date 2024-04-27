using Zoom_UI.MVVM.ViewModels;
namespace Zoom_UI.MVVM.Models;
#pragma warning disable CS8618


public interface IUser 
{
    public int Id { get; }
    public string Username { get; }
}


public class UserModel : IUser
{
    public int Id;
    public string Username;

    int IUser.Id => this.Id;
    string IUser.Username => this.Username;


    public UserModel() 
    {
        Id = -1;
        Username = string.Empty;
    }
    public UserModel(int id, string username)
    {
        Id = id;
        Username = username;
    }


    public UserViewModel AsViewModel()
    {
        return new UserViewModel(Username, Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is IUser vm)
        {
            return Id == vm.Id;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

#pragma warning restore CS8618
