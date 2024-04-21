using System.Windows.Media.Imaging;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.MVVM.ViewModels;

#pragma warning disable CS8618 

public class UserViewModel : ViewModelBase
{
    private User _user;
    private BitmapImage? _cameraImage;
    private bool _isMicrophoneOn;
    

    public bool IsCameraOn => CameraImage != null;

    public BitmapImage? CameraImage
    {
        get => _cameraImage;
        set
        {
            SetAndNotifyPropertyChanged(ref _cameraImage, value);
            OnPropertyChanged(nameof(IsCameraOn));
        }
    }

    public User User
    {
        get => _user;
        set => SetAndNotifyPropertyChanged(ref _user, value);
    }

    public bool IsMicrophoneOn
    {
        get => _isMicrophoneOn;
        set => SetAndNotifyPropertyChanged(ref _isMicrophoneOn, value);
    }
}

#pragma warning restore CS8618 
