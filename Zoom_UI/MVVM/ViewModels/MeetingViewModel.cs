using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WebEye.Controls.Wpf;
using Zoom_UI.Extensions;
using Zoom_UI.MVVM.Core;

namespace Zoom_UI.MVVM.ViewModels;

public class MeetingViewModel : ViewModelBase
{
    private string _message;
    private UserViewModel _selectedParticipant;

    public string MeetingId {  get; private set; }
    public UserViewModel CurrentUser {  get; private set; }
    public ObservableCollection<UserViewModel> Participants { get; } = new();
    public ObservableCollection<UserViewModel> ParticipantsSelection { get; } = new();


    public string Message
    {
        get => _message;
        set => SetAndNotifyPropertyChanged(ref _message, value);
    }

    public UserViewModel SelectedParticipant
    {
        get => _selectedParticipant;
        set => SetAndNotifyPropertyChanged(ref _selectedParticipant, value);  
    }


    public ICommand SendMessage { get; }
    public ICommand SwitchMicrophonState { get; }
    public ICommand SwitchCameraState { get; }
    public ICommand LeaveMeetingCommand {  get; }





    WebCameraControl? _webCameraControl;
    public MeetingViewModel(WebCameraControl webCameraControl)
    {
        _webCameraControl = webCameraControl;

        AddNewUser("1234", "Alex");
        AddNewUser("12345", "Henry");
        AddNewUser("123456", "Mark");
        AddNewUser("1", "Luke");
        AddNewUser("2", "Anna");
        AddNewUser("3", "Bill");
        AddNewUser("4", "Bill");
        AddNewUser("5", "Bill");
        AddNewUser("6", "Bill");
        AddNewUser("7", "Bill");
        AddNewUser("8", "Bill");
        Participants[0].CameraImage = new(new("pack://siteoforigin:,,,/Assets/cam_on.png", UriKind.Absolute));
        Participants[1].CameraImage = new(new("pack://siteoforigin:,,,/Assets/cam_on.png", UriKind.Absolute));
        Participants[2].CameraImage = new(new("pack://siteoforigin:,,,/Assets/cam_on.png", UriKind.Absolute));
        CurrentUser = Participants[1];



        SwitchMicrophonState = new RelayCommandWithoutParameters(() => 
        {
            CurrentUser.IsMicrophoneOn = !CurrentUser.IsMicrophoneOn;
            Message += "AB";
        });

        SwitchCameraState = new RelayCommandWithoutParameters(() =>
        {
            if(_webCameraControl != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_webCameraControl.IsCapturing)
                    {
                        _webCameraControl.StopCapture();
                    }
                    else
                    {
                        _webCameraControl.StartCapture(_webCameraControl.GetVideoCaptureDevices().ElementAt(1));
                    }
                });
            }
        });

        Task.Run(CaptureProcess);
    }


    private async void CaptureProcess()
    {
        try
        {
/*            while (true)
            {
                if (_webCameraControl?.IsCapturing ?? false)
                {
                    await Task.Delay(50);
                    // Invoke UI update code on the UI thread
                    *//*                await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        var frame = _webCameraControl?.GetCurrentImage();
                                        CurrentUser.CameraImage = frame?.ToBitmapImage();
                                    });*//*
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var frame = _webCameraControl?.GetCurrentImage();
                        CurrentUser.CameraImage = frame?.ToBitmapImage();
                        Participants[1].CameraImage = frame?.ToBitmapImage();
                    });
                }
                else
                {
                    CurrentUser.CameraImage = null;
                }
            }*/
        }
        catch (Exception ex)
        {
            var mes = ex.Message;
        }







        /*        try
                {

                    while (true)
                    {
                        await Task.Delay(400);
                        Dispatcher.CurrentDispatcher.Invoke(() =>
                        {
                            var frame = _webCameraControl?.GetCurrentImage();
                            CurrentUser.CameraImage = frame?.ToBitmapImage();
                        });
                    }
                }
                catch (Exception ex)
                {
                    var mes = ex.Message;
                }*/







        // Convert frame to binary array
        /*        byte[] frameData;
                using (MemoryStream ms = new MemoryStream())
                {
                    frame.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    frameData = ms.ToArray();
                }*/




        // Create video capture device
        /*        FilterInfoCollection captureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                VideoCaptureDevice videoDevice = new VideoCaptureDevice(captureDevices[0].MonikerString);

                // Attach NewFrame event handler which will be triggered for each new frame
                videoDevice.NewFrame += new NewFrameEventHandler(videoDevice_NewFrame);

                // Start capturing
                videoDevice.Start();

                void videoDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
                {
                    // Process frame here
                    Bitmap bitmap = eventArgs.Frame.Clone() as Bitmap;

                    // To display in a PictureBox named pictureBox
                    pictureBox.Image = bitmap;
                }*/
    }


















    private void AddNewUser(string uid, string username)
    {
        var existingUser = Participants.FirstOrDefault(x => x.UID == uid);

        if(existingUser != null)
        {
            existingUser.UID = uid;
            existingUser.Username = username;
        }
        else
        {
            var userViewModel = new UserViewModel(username, uid);
            AddUserToCollections(userViewModel);
        }
    }


    private void UpdateUserCameraFrame(string uid, BitmapImage bitmap)
    {
        var user = Participants.FirstOrDefault(x => x.UID == uid);

        if(user != null)
        {
            user.CameraImage = bitmap;
        }
    }


    private void AddUserToCollections(UserViewModel user)
    {
        if (user.UID == "All")
        {
            return;
        }

        Participants.Add(user);
        ParticipantsSelection.Add(user);
    }

    private void RemoveUserFromCollections(UserViewModel user)
    {
        if(user.UID == "All")
        {
            return;
        }

        Participants.Remove(user);
        ParticipantsSelection.Remove(user);
    }
}
