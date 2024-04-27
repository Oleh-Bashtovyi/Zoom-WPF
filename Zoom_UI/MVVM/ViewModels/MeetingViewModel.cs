using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WebEye.Controls.Wpf;
using Zoom_UI.MVVM.Core;
using Zoom_UI.MVVM.Models;
using Zoom_UI.Extensions;
using System.Drawing;
using Zoom_UI.ClientServer;
using System.Windows.Controls;
namespace Zoom_UI.MVVM.ViewModels;
#pragma warning disable CS8618

public class MeetingViewModel : ViewModelBase, ISeverEventSubsribable
{
    private readonly UserViewModel _everyone = new("Everyone", -1);
    private UserViewModel _selectedParticipant;
    private UserViewModel _currentUser;
    private ViewModelNavigator _navigator;
    private UdpComunicator _comunicator;
    private string _message;
    private string _theme;
    private int _meetingId;


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

    private WebCameraControl _webCamera;




    public WebCameraId _selectedWebCam;
    public ObservableCollection<WebCameraId> WebCameras { get; } = new();   
    public WebCameraId SelectedWebCamDevice
    {
        get => _selectedWebCam;
        set => SetAndNotifyPropertyChanged(ref _selectedWebCam, value);
    }


    //public MeetingViewModel(WebCameraControl webCameraControl)
    public MeetingViewModel(UdpComunicator comunicator, ViewModelNavigator navigator, UserViewModel currentUser, MeetingInfo meeting, WebCameraControl webCamera)
    {
        #region Commands_initialization
        CopyMeetingIdCommand =        new RelayCommand(CopyMeetingIdToClipboard);
        SwitchCameraStateCommand =    new RelayCommand(SwitchCameraState);
        SwitchMicrophonStateCommand = new RelayCommand(SwitchMicrophoneState);
        LeaveMeetingCommand =         new RelayCommand(LeaveMeeting);
        SendMessageCommand =          new RelayCommand(SendMessage, () => !string.IsNullOrWhiteSpace(Message) && SelectedParticipant != null);
        #endregion



        #region Initial_data
        CurrentUser = currentUser;
        ParticipantsSelection.Add(_everyone);
        AddUserToCollections(CurrentUser);
        SelectedParticipant = _everyone;
        _navigator = navigator;
        _comunicator = comunicator;
        _meetingId = meeting.Id;
        _webCamera = webCamera;
        CurrentTheme = "light";
        #endregion


        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach(var device in _webCamera.GetVideoCaptureDevices())
            {
                WebCameras.Add(device);    
            }
        });
        SelectedWebCamDevice = WebCameras[0];
        //CurrentUser.Id = 1000;
        //AddUserToCollections(CurrentUser);
    }





    private void SwitchMicrophoneState()
    {

    }


    private CancellationTokenSource CameraTokenSource;
    private void SwitchCameraState()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_webCamera.IsCapturing)
                {
                    CameraTokenSource?.Cancel();
                    _webCamera.StopCapture();
                }
                else
                {
                    CameraTokenSource = new CancellationTokenSource();
                    //var device = _webCamera.GetVideoCaptureDevices().ElementAt(0);
                    _webCamera.StartCapture(SelectedWebCamDevice);

                    Task.Run((async() => await CaptureProcess(CameraTokenSource.Token)));
                }
            });
        }
        catch ( Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(ex.Message);
                ErrorsList.Add(ex.Message);
            });
        }
    }


    private void LeaveMeeting()
    {
        try
        {
            Task.Run(async () =>
            {
                await _comunicator.SEND_LEAVE_MEETING(CurrentUser.Id, CurrentUser.Username);
            });
        }
        catch (Exception)
        {
        }
    }



    private void SendMessage()
    {

    }


















    private async Task CaptureProcess(CancellationToken token)
    {
        try
        {
            while (true)
            {
                await Application.Current.Dispatcher.InvokeAsync(async() =>
                {
                    if (_webCamera.IsCapturing)
                    {
                        var frame = _webCamera?.GetCurrentImage();

                        if (frame != null)
                        {
                            await _comunicator.SEND_CAMERA_FRAME(CurrentUser.Id, frame);
                            //CurrentUser.CameraImage = frame.AsBitmapImage();
                        }
                    }
                });
                await Task.Delay(80, token);
            }
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ErrorsList.Add(ex.Message);
            });
        }
        finally
        {
            //CurrentUser.CameraImage = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentUser.CameraImage = null;
            });
        }
    }







    #region Listener_events
    private void _listener_OnUserCreated(UserModel obj)
    {

        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentUser.Id = obj.Id;
            CurrentUser.Username = obj.Username;
            ErrorsList.Add($"Current user id: {CurrentUser.Id}");
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
            ErrorsList.Add($"Current user id: {CurrentUser.Id}");

        });

        Task.Run(async () => await _comunicator.Send_JoinedMeeting(CurrentUser.Id, obj.Id));

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
        var existingUser = Participants.FirstOrDefault(x => x.Id == model.Id);

        if (existingUser != null)
        {
            existingUser.Id = model.Id;
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
        var user = Participants.FirstOrDefault(x => x.Id == uid);

        if(user != null)
        {
            user.CameraImage = bitmap;
        }
    }
    private void AddUserToCollections(UserViewModel user)
    {
        if (user.Id <= 0)
        {
            return;
        }

        Participants.Add(user);
        ParticipantsSelection.Add(user);
    }
    private void RemoveUserFromCollections(UserViewModel user)
    {
        if(user.Id <= 0)
        {
            return;
        }

        Participants.Remove(user);
        ParticipantsSelection.Remove(user);
    }
    private void CopyMeetingIdToClipboard() => Clipboard.SetText(MeetingId.ToString());





    private void OnUserJoinedMeeting(UserModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddNewUser(model);
        });
    }

    private void OnUserLeftMeeting(UserModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if(model.Id == CurrentUser.Id)
            {
                _navigator.CurrentViewModel = new HomeViewModel(_comunicator, _navigator, _webCamera);
            }
            else 
            { 
                RemoveUserFromCollections(model.AsViewModel());
            }
        });
    }

    private void OnUSerFrameUpdated(CameraFrame frame)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
           UpdateUserCameraFrame(frame.UserId, frame.Image);
        });
    }



    void ISeverEventSubsribable.Subscribe()
    {
        _comunicator.OnUserJoinedMeeting += OnUserJoinedMeeting;
        _comunicator.OnUserLeftMeeting += OnUserLeftMeeting;
        _comunicator.OnCameraFrameUpdated += OnUSerFrameUpdated;

        Task.Run(async() => await _comunicator.Send_JoinedMeeting(CurrentUser.Id, _meetingId));
    }



    void ISeverEventSubsribable.Unsubscribe()
    {
        _comunicator.OnUserJoinedMeeting -= OnUserJoinedMeeting;
        _comunicator.OnUserLeftMeeting -= OnUserLeftMeeting;
        _comunicator.OnCameraFrameUpdated -= OnUSerFrameUpdated;
    }
}

#pragma warning restore CS8618