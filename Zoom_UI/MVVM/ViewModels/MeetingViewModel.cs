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
namespace Zoom_UI.MVVM.ViewModels;
#pragma warning disable CS8618

public class MeetingViewModel : ViewModelBase, ISeverEventSubsribable, IDisposable
{
    private readonly UserViewModel _everyone = new("Everyone", -1);
    private UserViewModel _selectedParticipant;
    private UserViewModel _currentUser;
    private ViewModelNavigator _navigator;
    private UdpComunicator _comunicator;
    private WebCameraControl _webCamera;
    private WebCameraId _selectedWebCam;
    private BitmapImage _screenDemonstrationImage;
    private UserViewModel _screenDemonstrator;
    private ApplicationData _applicationData;
    private CancellationTokenSource CameraTokenSource;
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
    public string CurrentTheme => _applicationData.ThemeManager.CurrentTheme;
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
    public UserViewModel ScreenDemonstrator
    {
        get => _screenDemonstrator;
        set => SetAndNotifyPropertyChanged(ref _screenDemonstrator, value);
    }
    public BitmapImage ScreenDemonstrationImage
    {
        get => _screenDemonstrationImage;
        set
        {
            SetAndNotifyPropertyChanged(ref _screenDemonstrationImage, value);
            OnPropertyChanged(nameof(IsDemonstrationActive));
        }
    }
    public bool IsDemonstrationActive => ScreenDemonstrationImage != null;
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
    public ICommand StartSharingScreenCommand {  get; }
    #endregion





    public ObservableCollection<WebCameraId> WebCameras { get; } = new();   
    public WebCameraId SelectedWebCamDevice
    {
        get => _selectedWebCam;
        set => SetAndNotifyPropertyChanged(ref _selectedWebCam, value);
    }


    public MeetingViewModel(ApplicationData data, MeetingInfo meeting)
    {
        #region Commands_initialization
        CopyMeetingIdCommand =        new RelayCommand(() => Clipboard.SetText(MeetingId.ToString()));
        SwitchCameraStateCommand =    new RelayCommand(SwitchCameraState);
        SwitchMicrophonStateCommand = new RelayCommand(SwitchMicrophoneState);
        LeaveMeetingCommand =         new RelayCommand(LeaveMeeting);
        StartSharingScreenCommand =   new RelayCommand(StartSharingScreen);
        ChangeThemeCommand = new RelayCommand(ChangeTheme);
        SendMessageCommand =          new RelayCommand(SendMessage, () => !string.IsNullOrWhiteSpace(Message) && SelectedParticipant != null);
        #endregion


        #region Initial_data
        _applicationData = data;
        CurrentUser = data.CurrentUser;
        ParticipantsSelection.Add(_everyone);
        AddUserToCollections(CurrentUser);
        SelectedParticipant = _everyone;
        _navigator = data.Navigator;
        _comunicator = data.Comunicator;
        _meetingId = meeting.Id;
        _webCamera = data.WebCamera;

        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var device in _webCamera.GetVideoCaptureDevices())
            {
                WebCameras.Add(device);
            }
        });
        SelectedWebCamDevice = WebCameras[0];
        #endregion
    }



    private void ChangeTheme()
    {
        try
        {
            _applicationData.ThemeManager.NextTheme();
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



    private void SwitchMicrophoneState()
    {

    }



    private void SwitchCameraState()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_webCamera.IsCapturing)
                {
                    CameraTokenSource?.Cancel();
                    CurrentUser.IsCameraOn = false;
                    _webCamera.StopCapture();
                    Task.Run(async () => await _comunicator.SEND_USER_TURN_OFF_CAMERA(CurrentUser.Id));
                }
                else
                {
                    CameraTokenSource = new CancellationTokenSource();
                    CurrentUser.IsCameraOn = true;
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
                await _comunicator.SEND_USER_LEAVE_MEETING(CurrentUser.Id, CurrentUser.Username);
            });
        }
        catch (Exception)
        {
        }
    }



    private void SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(Message))
        {
            Task.Run(() => _comunicator.SEND_MESSAGE(CurrentUser.Id, SelectedParticipant?.Id ?? -1, Message));
        }
    }


    private void StartSharingScreen()
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
                        var frame = _webCamera?.GetCurrentImage()?.ResizeBitmap(250, 250);
                        //var frame = Screenshot()?.ResizeBitmap(500, 500);
                        //var frame = Screenshot();

                        if (frame != null && CurrentUser.IsCameraOn)
                        {
                            await _comunicator.SEND_CAMERA_FRAME(CurrentUser.Id, frame);
                            //CurrentUser.CameraImage = frame.AsBitmapImage();
                            //ScreenDemonstrationImage = frame.AsBitmapImage();
                        }
                    }
                });
                await Task.Delay(60, token);
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





    public Bitmap Screenshot()
    {
        // Get the size of the primary screen using SystemParameters
/*        int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        int screenHeight = (int)SystemParameters.PrimaryScreenHeight;*/
        int screenWidth = 1920;
        int screenHeight = 1080;

        Bitmap screenshot = new Bitmap(screenWidth, screenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(screenshot))
        {
            graphics.CopyFromScreen(
                0, 0, 0, 0, 
                new System.Drawing.Size(screenWidth, screenHeight));
        }
        return screenshot;
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



    private void AddUserToCollections(UserViewModel user)
    {
        Participants.Add(user);
        ParticipantsSelection.Add(user);
    }
    private void RemoveUserFromCollections(UserViewModel user)
    {
        Participants.Remove(user);
        ParticipantsSelection.Remove(user);
    }




    #region SERVER_EVENTS
    //PARTICIPATING
    //===========================================================
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
                _navigator.CurrentViewModel = new HomeViewModel(_applicationData);
            }
            else 
            { 
                RemoveUserFromCollections(model.AsViewModel());
            }
        });
    }

    //CAMERA FRAME
    //===========================================================
    private void OnUserCameraFrameFrameUpdated(CameraFrame frame)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var user = Participants.FirstOrDefault(x => x.Id == frame.UserId);

            if (user != null)
            {
                user.CameraImage = frame.Image;
            }
        });
    }
    private void OnUserCameraTurnedOn(UserModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var user = Participants.FirstOrDefault(x => x.Id == model.Id);

            if (user != null)
            {
                user.IsCameraOn = true;
            }
        });
    }
    private void OnUserCameraTurnedOff(UserModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var user = Participants.FirstOrDefault(x => x.Id == model.Id);

            if (user != null)
            {
                user.IsCameraOn = false;
            }
        });
    }

    //MESSAGES
    //===========================================================
    private void OnMessagereceived(MessageInfo message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var from = Participants.FirstOrDefault(x => x.Id == message.SenderId) ?? new();
            var to = Participants.FirstOrDefault(x => x.Id == message.ReceiverId) ?? _everyone;
            AddNewMessage(from, to, message?.Content ?? string.Empty);
        });
    }
    private void OnErrorReceived(ErrorModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(model.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ErrorsList.Add(model.Message ?? string.Empty);
        });
    }

    //EVENTS SUBSCRIPTION
    //===========================================================
    private void NotifyThemeChanged(string newTheme)
    {
        OnPropertyChanged(nameof(CurrentTheme));
    }
    void ISeverEventSubsribable.Subscribe()
    {
        //participating
        _comunicator.OnUser_JoinedMeeting += OnUserJoinedMeeting;
        _comunicator.OnUser_LeftMeeting += OnUserLeftMeeting;
        //camera frames
        _comunicator.OnCameraFrameOfUserUpdated += OnUserCameraFrameFrameUpdated;
        _comunicator.OnUser_TurnedCamera_OFF += OnUserCameraTurnedOff;
        _comunicator.OnUser_TurnedCamera_ON += OnUserCameraTurnedOn;
        //messages
        _comunicator.OnMessageSent += OnMessagereceived;
        _comunicator.OnErrorReceived += OnErrorReceived;
        _applicationData.ThemeManager.OnThemeChanged += NotifyThemeChanged;

        Task.Run(async() => await _comunicator.SEND_USER_JOINED_MEETING(CurrentUser.Id, _meetingId));
    }
    void ISeverEventSubsribable.Unsubscribe()
    {
        //participating
        _comunicator.OnUser_JoinedMeeting -= OnUserJoinedMeeting;
        _comunicator.OnUser_LeftMeeting -= OnUserLeftMeeting;
        //camera frames
        _comunicator.OnCameraFrameOfUserUpdated -= OnUserCameraFrameFrameUpdated;
        _comunicator.OnUser_TurnedCamera_OFF -= OnUserCameraTurnedOff;
        _comunicator.OnUser_TurnedCamera_ON -= OnUserCameraTurnedOn;
        //messages
        _comunicator.OnMessageSent -= OnMessagereceived;
        _comunicator.OnErrorReceived -= OnErrorReceived;
        _applicationData.ThemeManager.OnThemeChanged -= NotifyThemeChanged;
    }

    public void Dispose()
    {
        var vm = this as ISeverEventSubsribable;
        vm.Unsubscribe();

        CameraTokenSource?.Dispose();

        Task.Run(async () => await _applicationData.Comunicator.SEND_USER_LEAVE_MEETING(CurrentUser.Id, CurrentUser.Username));
    }
    #endregion
}

#pragma warning restore CS8618