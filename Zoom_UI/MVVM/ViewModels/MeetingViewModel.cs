using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WebEye.Controls.Wpf;
using Zoom_UI.MVVM.Core;
using Zoom_UI.MVVM.Models;
using System.IO;
using Zoom_Server.Net;
using Zoom_UI.Extensions;
using System.Drawing;
using System.Windows.Threading;
using Zoom_UI.ClientServer;
using Zoom_Server.Logging;
using static Zoom_Server.Net.PacketReader;
using System.Windows.Media.Media3D;
using AForge.Video.DirectShow;
using AForge.Video;
namespace Zoom_UI.MVVM.ViewModels;
#pragma warning disable CS8618

public class MeetingViewModel : ViewModelBase
{
    private readonly UserViewModel _everyone = new("Everyone", -1);
    private readonly UserViewModel _none = new("", -1);
    private UserViewModel _selectedParticipant;
    private UserViewModel _currentUser;
    private UdpComunicator _listener;
    private string _message;
    private string _theme;
    private int _meetingId;
    private int _serverPort = 9999;
    private string _serverIP = "127.0.0.1";

    #region PROPERTIES
    public string Message
    {
        get => _message;
        set => SetAndNotifyPropertyChanged(ref _message, value);
    }
    public int MeetingId 
    {
        get => _meetingId;
        set => SetAndNotifyPropertyChanged(ref  _meetingId, value);
    }
    public string CurrentTheme
    {
        get => _theme;
        set => SetAndNotifyPropertyChanged(ref _theme, value);
    }
    public UserViewModel CurrentUser 
    {
        get => _currentUser;
        set => SetAndNotifyPropertyChanged(ref _currentUser, value);
    }
    public UserViewModel SelectedParticipant
    {
        get => _selectedParticipant;
        set => SetAndNotifyPropertyChanged(ref _selectedParticipant, value);  
    }
    #endregion

    #region COLLECCTIONS
    public ObservableCollection<string> ErrorsList { get; } = new();
    public ObservableCollection<UserViewModel> Participants { get; } = new();
    public ObservableCollection<UserViewModel> ParticipantsSelection { get; } = new();
    public ObservableCollection<MessageModel> ParticipantsMessages { get; } = new();
    #endregion

    #region COMMANDS
    public ICommand SendMessageCommand { get; }
    public ICommand CopyMeetingIdCommand { get; }
    public ICommand SwitchMicrophonStateCommand { get; }
    public ICommand SwitchCameraStateCommand { get; }
    public ICommand LeaveMeetingCommand {  get; }
    public ICommand ChangeThemeCommand {  get; }
    #endregion




    WebCameraControl? _webCameraControl;

    public MeetingViewModel(WebCameraControl webCameraControl)
    {
        #region Commands_initialization
        CopyMeetingIdCommand =        new RelayCommand(CopyMeetingIdToClipboard);
        SwitchCameraStateCommand =    new RelayCommand(SwitchCameraState);
        SwitchMicrophonStateCommand = new RelayCommand(SwitchMicrophoneState);
        LeaveMeetingCommand =         new RelayCommand(LeaveMeeting);
        SendMessageCommand =          new RelayCommand(SendMessage, () => !string.IsNullOrWhiteSpace(Message) && SelectedParticipant != null);
        #endregion

        #region Listener_initialization
        _listener = new(_serverIP, _serverPort, new LoggerWithCollection(ErrorsList));
        _listener.OnUserCreated +=                 _listener_OnUserCreated;
        _listener.OnMeetingCreated +=              _listener_OnMeetingCreated;
        _listener.OnUserJoinedMeeting +=           _listener_OnUserJoinedMeeting;
        _listener.onUserLeftMeeting +=             _listener_onUserLeftMeeting;
        _listener.OnCameraFrameUpdated +=          _listener_OnCameraFrameUpdated;
        _listener.OnUserJoinedMeeting_UsingCode += _listener_OnUserJoinedMeeting_UsingCode; 
        #endregion

        #region Initial_data
        ParticipantsSelection.Add(_everyone);
        SelectedParticipant = _everyone;
        CurrentTheme = "light";
        CurrentUser = new UserViewModel(string.Empty, 1);
        CurrentUser.IsMicrophoneOn = false;
        _listener.Run();
        #endregion

        AddUserToCollections(CurrentUser);
        Message = "USER_1";

        _webCameraControl = webCameraControl;
        ChangeThemeCommand = new RelayCommand(() =>
        {
            try
            {
                AddNewMessage(_everyone, _everyone, "Sending begun!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                ErrorsList.Add(ex.Message);
            }
        });

        Task.Run(CaptureProcess);
        //StartCamera();
    }



/*    private async Task CaptureProcess()
    {
        try
        {
            while (true)
            {
                await Task.Delay(20);
                Bitmap? frame = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    frame = _webCameraControl?.GetCurrentImage();

                    if (frame != null)
                    {
                        //await _listener.Send_CameraFrame(CurrentUser.UID, frame);

                        CurrentUser.CameraImage = frame.AsBitmapImage();
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ErrorsList.Add(ex.Message);
            });
        }
    }
*/






    private void SwitchMicrophoneState()
    {
        ErrorsList.Add("Trying to create user..");
        Task.Run(async () => await _listener.Send_CrateUser(Message));
        ErrorsList.Add("Creation request send!");
    }


    private void SwitchCameraState()
    {
        Task.Run(_listener.Send_CreateMeeting);
        /*        var frame = new Bitmap( "E:\\downloads\\ggg.png");
                Task.Run(async () => await _listener.Send_CameraFrame(Participants[1].UID, frame));*/
    }


    private void LeaveMeeting()
    {
        try
        {
            int code = int.Parse(Message);
            Task.Run(async() => await _listener.Send_JoinUsingMeetingUsingCode(code));
        }
        catch (Exception)
        {
        }
    }



    private void SendMessage()
    {
/*        Application.Current.Dispatcher.Invoke(() =>
        {
            var _webCameraId = _webCameraControl?.GetVideoCaptureDevices().ElementAt(1);
            _webCameraControl?.StartCapture(_webCameraId);
        });

        Task.Run(CaptureProcess);*/
    }


















    private async void CaptureProcess()
    {
        try
        {
            while (true)
            {
                await Task.Delay(20);
                Bitmap? frame = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    frame = _webCameraControl?.GetCurrentImage();

                    if (frame != null)
                    {
                        /*                    await _listener.Send_CameraFrame(CurrentUser.UID, frame);   */

                        CurrentUser.CameraImage = frame.AsBitmapImage();
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ErrorsList.Add(ex.Message);
            });
        }
    }




/*    private void StartCamera()
    {
        var VideoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        var device = VideoDevices[0];
        _videoSource = new VideoCaptureDevice(device.MonikerString);
        _videoSource.NewFrame += video_NewFrame;
        _videoSource.Start();

    }*/


    private VideoCaptureDevice _videoSource;







    private void video_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
    {
        try
        {
            BitmapImage bi;
            using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
            {
                bi = bitmap.AsBitmapImage();
            }
            bi.Freeze(); // avoid cross thread operations and prevents leaks
            Application.Current.Dispatcher.Invoke(() => { CurrentUser.CameraImage = bi; });

        }
        catch (Exception exc)
        {
            MessageBox.Show("Error on _videoSource_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _videoSource.SignalToStop();
            _videoSource.NewFrame -= new NewFrameEventHandler(video_NewFrame);
            //StopCamera();
        }
    }















































    #region Listener_events
    private void _listener_OnUserCreated(UserModel obj)
    {

        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentUser.UID = obj.UID;
            CurrentUser.Username = obj.Username;
            ErrorsList.Add($"Current user id: {CurrentUser.UID}");
        });
    }
    private void _listener_OnMeetingCreated(MeetingInfo obj)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MeetingId = obj.Id;
        });
    }
    private void _listener_OnUserJoinedMeeting_UsingCode(MeetingInfo obj)
    {
        Application.Current.Dispatcher?.Invoke(() =>
        {
            ErrorsList.Add($"Current user id: {CurrentUser.UID}");

        });

        Task.Run(async () => await _listener.Send_JoinedMeeting(CurrentUser.UID, obj.Id));

    }
    private void _listener_OnUserJoinedMeeting(UserModel obj)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddNewUser(obj.UID, obj.Username);
        });
    }
    private void _listener_OnCameraFrameUpdated(CameraFrame obj)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateUserCameraFrame(obj.UserId, obj.Frame);
        });
    }


    private void _listener_onUserLeftMeeting(UserModel obj)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            RemoveUserFromCollections(obj.AsViewModel());
        });
    }
    #endregion





    private void ReplaceTheme(string newTheme)
    {
        var newThemeDict = new ResourceDictionary()
        {
            Source = new Uri($"Themes/{newTheme}.xaml", UriKind.Relative)
        };


        ResourceDictionary oldTheme = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source.OriginalString == "Themes/LightTheme.xaml");

        if (oldTheme != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(oldTheme);
        }
        Application.Current.Resources.MergedDictionaries.Add(newThemeDict);
    }
    private void AddNewMessage(UserViewModel from, UserViewModel to, string content)
    {
        var message = new MessageModel();
        message.Content = content;
        message.When = DateTime.Now;
        message.From = from.Username;
        message.To = to.Username;
        ParticipantsMessages.Add(message);
    }

    private void AddNewUser(UserModel model)
    {
        var existingUser = Participants.FirstOrDefault(x => x.UID == model.UID);

        if (existingUser != null)
        {
            existingUser.UID = model.UID;
            existingUser.Username = model.Username;
        }
        else
        {
            var userViewModel = model.AsViewModel();    
            AddUserToCollections(userViewModel);
        }
    }
    private void AddNewUser(int uid, string username)
    {
        AddNewUser(new UserModel(uid, username));
    }
    private void UpdateUserCameraFrame(int uid, BitmapImage bitmap)
    {
        var user = Participants.FirstOrDefault(x => x.UID == uid);

        if(user != null)
        {
            user.CameraImage = bitmap;
        }
    }
    private void AddUserToCollections(UserViewModel user)
    {
        if (user.UID <= 0)
        {
            return;
        }

        Participants.Add(user);
        ParticipantsSelection.Add(user);
    }
    private void RemoveUserFromCollections(UserViewModel user)
    {
        if(user.UID <= 0)
        {
            return;
        }

        Participants.Remove(user);
        ParticipantsSelection.Remove(user);
    }
    private void CopyMeetingIdToClipboard() => Clipboard.SetText(MeetingId.ToString());
    private void InitializationWithTestData()
    {
        AddNewUser(1, "Alex");
        AddNewUser(2, "Henry");
        AddNewUser(3, "Mark");
        AddNewUser(4, "Luke");
        AddNewUser(5, "Anna");
        AddNewUser(6, "Bill");
        AddNewUser(7, "Bill");
        AddNewUser(8, "Bill");
        AddNewUser(9, "Bill");
        AddNewUser(10, "Bill");
        AddNewUser(11, "Bill");
        ErrorsList.Add("Some error");
        ErrorsList.Add("Some error 2");
        ErrorsList.Add("Some very biiiig error that IDK. and dfffgfgggdg..... difjsdgojdskglj g jdgskdg ;gdsgoigjgsdpgjigoj");
        ErrorsList.Add("Some error 2");
        ErrorsList.Add("Some error 2");
        ErrorsList.Add("Some error 2");
        ErrorsList.Add("Some error 2");
        Participants[0].CameraImage = new(new("pack://siteoforigin:,,,/assets/cam_on.png", UriKind.Absolute));
        Participants[1].CameraImage = new(new("pack://siteoforigin:,,,/assets/cam_on.png", UriKind.Absolute));
        Participants[2].CameraImage = new(new("pack://siteoforigin:,,,/assets/cam_on.png", UriKind.Absolute));
        AddNewMessage(Participants[0], _everyone, "Hello everyone!");
        AddNewMessage(Participants[0], Participants[0], "Hello this must be visible only to one user!");
    }
}

#pragma warning restore CS8618