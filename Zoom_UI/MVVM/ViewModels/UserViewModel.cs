using System.Diagnostics.Tracing;
using System.Windows.Media.Imaging;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.MVVM.ViewModels;

#pragma warning disable CS8618 

public class UserViewModel : ViewModelBase
{
    private string _uid;
    private string _username;
    private BitmapImage? _cameraImage;
    private bool _isMicrophoneOn;


    public string ShortName => GetShortName();
    public bool IsCameraOn => CameraImage != null;


    public string UID
    {
        get => _uid;
        set => SetAndNotifyPropertyChanged(ref _uid, value);
    }

    public bool IsMicrophoneOn
    {
        get => _isMicrophoneOn;
        set => SetAndNotifyPropertyChanged(ref _isMicrophoneOn, value);
    }

    public string Username
    {
        get => _username;
        set
        {
            SetAndNotifyPropertyChanged(ref _username, value);
            OnPropertyChanged(nameof(ShortName));
        }
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




    public UserViewModel() { }
    public UserViewModel(string username, string uid)
    {
        Username = username;
        UID = uid;
    }



    private string GetShortName()
    {
        if (string.IsNullOrWhiteSpace(_username))
        {
            return string.Empty;
        }

        var words = _username.Split(' ').Take(2).ToArray();

        if (words.Length == 2)
        {
            return words[0].ToUpper() + words[1].ToUpper();
        }

        return words[0].ToUpper();
    }


    public UserModel AsModel() => new UserModel(UID, Username);
}

#pragma warning restore CS8618 
