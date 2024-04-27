using System.Windows.Media.Imaging;
using Zoom_UI.MVVM.Models;
namespace Zoom_UI.MVVM.ViewModels;
#pragma warning disable CS8618 


public class UserViewModel : ViewModelBase, IUser
{
    private UserModel _model;
    private bool _isMicrophoneOn;
    private BitmapImage? _cameraImage;



    #region Properties
    public int Id
    {
        get => _model.Id;
        set => SetAndNotifyPropertyChanged(ref _model.Id, value);
    }
    public string Username
    {
        get => _model.Username;
        set
        {
            SetAndNotifyPropertyChanged(ref _model.Username, value);
            OnPropertyChanged(nameof(ShortName));
        }
    }
    public bool IsMicrophoneOn
    {
        get => _isMicrophoneOn;
        set => SetAndNotifyPropertyChanged(ref _isMicrophoneOn, value);
    }
    public BitmapImage? CameraImage
    {
        get => _cameraImage;
        set
        {
            SetAndNotifyPropertyChanged(ref _cameraImage, value);
            OnPropertyChanged(nameof(IsCameraOn));
        }
    }
    public string ShortName => GetShortName();
    public bool IsCameraOn => CameraImage != null;
    #endregion



    public UserViewModel() 
    {
        _model = new UserModel();
    }
    public UserViewModel(string username, int id) : this() 
    {
        Username = username;
        Id = id;
    }


    private string GetShortName()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            return string.Empty;
        }

        var words = Username.Split(' ').Take(2).ToArray();

        if (words.Length == 2)
        {
            var a = words[0][0];
            var b = words[1][0];
            return (a.ToString() + b).ToUpper();
        }

        return words[0][0].ToString().ToUpper();
    }

    public UserModel AsModel() => new UserModel(Id, Username);


    public override bool Equals(object? obj)
    {
        if(obj is  IUser vm)
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
