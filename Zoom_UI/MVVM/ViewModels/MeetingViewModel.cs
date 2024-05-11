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
using Microsoft.Win32;
using System.IO;
using Zoom_UI.Managers;
using System.Windows.Media;
using NAudio.Wave;
using static Zoom_UI.ClientServer.UdpComunicator;
using Zoom_Server.Net.Codes;
namespace Zoom_UI.MVVM.ViewModels;
#pragma warning disable CS8618

public class MeetingViewModel : ViewModelBase, ISeverEventSubsribable, IDisposable
{
    private readonly UserViewModel _everyone = new("Everyone", -1);
    private UserViewModel _selectedParticipant;
    private UserViewModel _currentUser;
    private ViewModelNavigator _navigator;
    private UdpComunicator _comunicator;
    private WebCameraCaptureManager _webCamera;
    private WebCameraId _selectedWebCam;
    private BitmapImage _screenDemonstrationImage;
    private UserViewModel _screenDemonstrator;
    private ApplicationData _applicationData;
    private ScreenCaptureManager _screenCaptureManager;
    private CancellationTokenSource _meetingTokenSource;
    private bool _isDemonstrationActive;
    private string _message;
    private int _meetingId;
    private int _sellectedAudioDeviceIndex;

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
        set => SetAndNotifyPropertyChanged(ref _screenDemonstrationImage, value);
    }
    public WebCameraId SelectedWebCamDevice
    {
        get => _selectedWebCam;
        set => SetAndNotifyPropertyChanged(ref _selectedWebCam, value);
    }
    public bool IsDemonstrationActive
    {
        get => _isDemonstrationActive;
        set => SetAndNotifyPropertyChanged(ref _isDemonstrationActive, value);
    }
    public int SellectedAudioDeviceIndex
    {
        get => _sellectedAudioDeviceIndex;
        set => SetAndNotifyPropertyChanged(ref _sellectedAudioDeviceIndex, value);
    }
    #endregion

    #region COLLECCTIONS
    public ObservableCollection<DebugMessage> ErrorsList { get; } 
    public ObservableCollection<UserViewModel> Participants { get; } = new();
    public ObservableCollection<UserViewModel> ParticipantsSelection { get; } = new();
    public ObservableCollection<MessageModel> ParticipantsMessages { get; } = new();
    public ObservableCollection<WebCameraId> WebCameras { get; } = new();   
    public ObservableCollection<WaveInCapabilities> AudioInputDevices { get; } = new();   
    #endregion

    #region COMMANDS
    public ICommand SendMessageCommand { get; }
    public ICommand SendFileCommand { get; }
    public ICommand CopyMeetingIdCommand { get; }
    public ICommand SwitchMicrophonStateCommand { get; }
    public ICommand SwitchCameraStateCommand { get; }
    public ICommand LeaveMeetingCommand {  get; }
    public ICommand ChangeThemeCommand {  get; }
    public ICommand StartSharingScreenCommand {  get; }
    public ICommand DownloadFileCommand {  get; }
    #endregion



    public MeetingViewModel(ApplicationData data, MeetingInfo meeting)
    {
        #region Commands_initialization
        SendFileCommand = new RelayCommand(SendFile, () => SelectedParticipant != null);
        SwitchCameraStateCommand =    new RelayCommand(SwitchCameraState);
        SwitchMicrophonStateCommand = new RelayCommand(SwitchMicrophoneState);
        LeaveMeetingCommand =         new RelayCommand(LeaveMeeting);
        StartSharingScreenCommand =   new RelayCommand(StartSharingScreen);
        ChangeThemeCommand =          new RelayCommand(ChangeTheme);
        CopyMeetingIdCommand =        new RelayCommand(
            () => Clipboard.SetText(MeetingId.ToString()), 
            () => !string.IsNullOrWhiteSpace(MeetingId.ToString()));
        SendMessageCommand =          new RelayCommand(
            () => SendMessage(), 
            () => !string.IsNullOrWhiteSpace(Message) && SelectedParticipant != null);


        #endregion

        #region Initial_data
        _applicationData = data;
        CurrentUser = data.CurrentUser;
        ParticipantsSelection.Add(_everyone);
        Participants.Add(CurrentUser);
        SelectedParticipant = _everyone;
        _navigator = data.Navigator;
        _comunicator = data.Comunicator;
        _meetingId = meeting.Id;
        _webCamera = data.WebCamera;
        ErrorsList = data.ErrorsBuffer;
        OnPropertyChanged(nameof(ErrorsList));
        _screenCaptureManager = data.ScreenCaptureManager;
        _meetingTokenSource = new CancellationTokenSource();
        DownloadFileCommand = new FileRelayCommand(DownloadFile);

        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var device in _webCamera.GetInputDevices())
            {
                WebCameras.Add(device);
            }
        });

        foreach(var device in _applicationData.MicrophoneCaptureManager.GetInputDevices())
        {
            AudioInputDevices.Add(device);
        }
        if(AudioInputDevices.Count > 0)
        {
            SellectedAudioDeviceIndex = 0;
        }
        if (WebCameras.Count > 0)
        {
            SelectedWebCamDevice = WebCameras.First();
        }
        waveProvider = new BufferedWaveProvider(_applicationData.MicrophoneCaptureManager.GetWaveFormat);
        _waveOut.Init(waveProvider);
        _waveOut.Play();
        #endregion
    }

    private WaveOut _waveOut = new();
    private BufferedWaveProvider waveProvider;



    private void DownloadFile(FIleModel file)
    {
        var openFolderDialog = new OpenFolderDialog();

        openFolderDialog.Title = "Select path to save file";

        if (openFolderDialog.ShowDialog() ?? false)
        {
            var savePath = Path.Combine(openFolderDialog.FolderName, file.FileName);

            File.WriteAllBytes(savePath, file.Data);
        }
    }


    private void SendFile()
    {
        var openFileDialog = new OpenFileDialog();

        openFileDialog.Title = "Select file to send";

        if (openFileDialog.ShowDialog() ?? false)
        {
            if(openFileDialog.FileName != null)
            {
                Task.Run(async () => await _comunicator.SEND_FILE(CurrentUser.Id, SelectedParticipant.Id, openFileDialog.FileName));
            }
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



    private void SwitchMicrophoneState()
    {
        if(_applicationData.MicrophoneCaptureManager.IsMicrophonTurnedOn)
        {
            _applicationData.MicrophoneCaptureManager.StopRecording();
        }
        else if (SellectedAudioDeviceIndex >= 0)
        {
            _applicationData.MicrophoneCaptureManager.StartRecording(SellectedAudioDeviceIndex);
        }
    }



    private void SwitchCameraState()
    {
        try
        {
            if (CurrentUser.IsCameraOn)
            {
                _webCamera.StopCapturing();
            }
            else
            {
                _webCamera.StartCapturing(SelectedWebCamDevice, 20);
            }
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(ex.Message);
                ErrorsList.Add(new(ex.Message));
            });
        }
    }


    private void StartSharingScreen()
    {
        if (IsDemonstrationActive)
        {
            _screenCaptureManager.StopCapturing();
        }
        else
        {
            Task.Run(async() => await _comunicator.SEND_REQUEST_FOR_SCREEN_DEMONSTRATION(CurrentUser.Id));
        }
    }










    private void AddNewMessage(UserViewModel from, UserViewModel to, object content)
    {
        var message = new MessageModel();
        message.Content = content;
        message.When = DateTime.Now;
        message.From = from.Username;
        message.To = to.Username;
        ParticipantsMessages.Add(message);
    }

    private void AddUserToMeeting(UserModel model)
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
            
            Participants.Add(userViewModel);

            if(userViewModel.Id != CurrentUser.Id)
            {
                ParticipantsSelection.Add(userViewModel);
            }
        }
    }

    private void RemoveUserFromMeeting(UserModel model)
    {
        var existingUser = Participants.FirstOrDefault(x => x.Id == model.Id);

        if(existingUser != null)
        {
            RemoveUserFromCollections(existingUser);

            if(ScreenDemonstrator?.Id == model.Id)
            {
                ScreenDemonstrator = null;
                ScreenDemonstrationImage = null;
                IsDemonstrationActive = false;
            }
        }
    }

    private void ChangeTheme()
    {
        try
        {
            _applicationData.ThemeManager.NextTheme();
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(ex.Message);
                ErrorsList.Add(new(ex.Message));
            });
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
    private void Comunicator_OnUserJoinedMeeting(UserModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddUserToMeeting(model);
        });
    }
    private void Comunicator_OnUserLeftMeeting(UserModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if(model.Id == CurrentUser.Id)
            {
                _navigator.CurrentViewModel = new HomeViewModel(_applicationData);
                ((ISeverEventSubsribable)this).Unsubscribe();
            }
            else 
            {
                RemoveUserFromMeeting(model);
            }
        });
    }

    //CAMERA FRAME
    //===========================================================
    private void Comunicator_OnCameraFrameReceived(ImageFrame frame)
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
    private void Comunicator_OnCameraTurnedOn(UserModel model)
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
    private void Comunicator_OnCameraTurnedOff(UserModel model)
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
    private void WebCameraManager_OnCaptureStarted()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentUser.IsCameraOn = true;
        });
    }
    private void WebCameraManager_OnCaptureFinished()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentUser.IsCameraOn = false;
            Task.Run(async () => await _comunicator.SEND_USER_TURN_OFF_CAMERA(CurrentUser.Id));
        });
    }
    private void WebCameraManager_OnFrameCaptured(Bitmap? bitmap)
    {
        if(bitmap != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //CurrentUser.CameraImage = bitmap.AsBitmapImage();
                Task.Run(async () => await _comunicator.SEND_CAMERA_FRAME(CurrentUser.Id, MeetingId, bitmap.ResizeBitmap(250, 250)));
            });
        }
    }


    //SCREEN CAPTURE FRAME
    //===========================================================
    private void Comunicator_OnScreenDemonstrationStarted(UserModel performer)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var demonstrator = Participants.FirstOrDefault(x => x.Id ==  performer.Id);
            ScreenDemonstrator = demonstrator;
            IsDemonstrationActive = true;
        });
    }
    private void Comunicator_OnScreenDemonstrationFinished(UserModel performer)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsDemonstrationActive = false;
            ErrorsList.Add(new("Recived command from comunicator to stop demostrating!"));
        });
    }
    private void Comunicator_OnScreenFrameReceived(ImageFrame frame)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ScreenDemonstrationImage = frame.Image;
        });
    }
    private void ScreenCaptureManager_OnCaptureStarted()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ScreenDemonstrator = CurrentUser;
            IsDemonstrationActive = true;
        });
    }

    private void ScreenCaptureManager_OnCaptureFinished()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsDemonstrationActive = false;
            ErrorsList.Add(new("Screen capture manager finished capture process!"));
            Task.Run(async () => await _comunicator.SEND_USER_TURN_OFF_DEMONSTRATION(CurrentUser.Id));
        });
    }

    private void ScreenCaptureManager_OnFrameCaptured(Bitmap? bitmap)
    {
        if(bitmap != null)
        {
            _comunicator.SEND_SCREEN_IMAGE(CurrentUser.Id, bitmap.ResizeBitmap(1000, 1000));


            //Task.Run(async () => await _comunicator.SEND_SCREEN_IMAGE(CurrentUser.Id, bitmap.ResizeBitmap(1000, 1000)));
            //_comunicator.SEND_SCREEN_IMAGE(CurrentUser.Id, bitmap.ResizeBitmap(1000, 1000));
            //ScreenDemonstrationImage = bitmap.AsBitmapImage();
        }
    }




    //MESSAGES
    //===========================================================
    private void Comunicator_OnMessageReceived(MessageInfo message)
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
        if(model.ErrorCode == ErrorCode.SCREEN_CAPTURE_DOES_NOT_ALLOWED)
        {
            try
            {
                _screenCaptureManager.StopCapturing();
            }
            catch (Exception)
            {
            }
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(model.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ErrorsList.Add(new(model.Message ?? string.Empty));
        });
    }
    private void OnSuccessReceived(SuccessModel model)
    {
        if (model.SuccessCode == ScsCode.SCREEN_DEMONSTRATION_ALLOWED)
        {
            _screenCaptureManager.StartCapturing();
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(model.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }




    private void MicrophonManager_CaptureStarted()
    {
        CurrentUser.IsMicrophoneOn = true;
    }
    private void MicrophonManager_SoundReceived(byte[] soundBytes)
    {
        _comunicator.SEND_AUDIO(new(CurrentUser.Id, soundBytes));
    }
    private void MicrophonManager_CaptureFinished()
    {
        CurrentUser.IsMicrophoneOn = false;
    }



    private void Comunicator_UserTurnedMicrophoneOn(UserModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var user = Participants.FirstOrDefault(x => x.Id == model.Id);

            if(user != null)
            {
                user.IsMicrophoneOn = true;
            }
        });
    }

    private void Comunicator_UserTurnedMicrophoneOff(UserModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var user = Participants.FirstOrDefault(x => x.Id == model.Id);

            if (user != null)
            {
                user.IsMicrophoneOn = false;
            }
        });
    }

    private void Comunicator_SoundReceived(AudioFrame audioFrame)
    {
        if(audioFrame.UserId == CurrentUser.Id)
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            waveProvider.AddSamples(audioFrame.Data, 0, audioFrame.Data.Length);
        });
    }






    //EVENTS SUBSCRIPTION
    //===========================================================
    private void NotifyThemeChanged(string newTheme) => OnPropertyChanged(nameof(CurrentTheme));
    void ISeverEventSubsribable.Subscribe()
    {
        //participating
        //===========================================================================
        _comunicator.OnUser_JoinedMeeting += Comunicator_OnUserJoinedMeeting;
        _comunicator.OnUser_LeftMeeting += Comunicator_OnUserLeftMeeting;
        //camera frames
        //===========================================================================
        _comunicator.OnCameraFrameOfUserUpdated += Comunicator_OnCameraFrameReceived;
        _comunicator.OnUser_TurnedCamera_OFF += Comunicator_OnCameraTurnedOff;
        _comunicator.OnUser_TurnedCamera_ON += Comunicator_OnCameraTurnedOn;
        _webCamera.OnError += OnErrorReceived;
        _webCamera.OnCaptureStarted += WebCameraManager_OnCaptureStarted;
        _webCamera.OnCaptureFinished += WebCameraManager_OnCaptureFinished;
        _webCamera.OnImageCaptured += WebCameraManager_OnFrameCaptured;
        //screen capture
        //===========================================================================
        _comunicator.OnScreenDemonstrationFrameOfUserUpdated += Comunicator_OnScreenFrameReceived;
        _comunicator.OnUser_TurnedDemonstration_ON += Comunicator_OnScreenDemonstrationStarted;
        _comunicator.OnUser_TurnedDemonstration_OFF += Comunicator_OnScreenDemonstrationFinished;
        _screenCaptureManager.OnError += OnErrorReceived;
        _screenCaptureManager.OnCaptureStarted += ScreenCaptureManager_OnCaptureStarted;
        _screenCaptureManager.OnCaptureFinished += ScreenCaptureManager_OnCaptureFinished;
        _screenCaptureManager.OnImageCaptured += ScreenCaptureManager_OnFrameCaptured;
        //messages
        //===========================================================================
        _comunicator.OnMessageSent += Comunicator_OnMessageReceived;
        _comunicator.OnErrorReceived += OnErrorReceived;
        _comunicator.OnSuccessReceived += OnSuccessReceived;
        _applicationData.ThemeManager.OnThemeChanged += NotifyThemeChanged;
        //audio
        //===========================================================================
        _applicationData.MicrophoneCaptureManager.OnSoundCaptured += MicrophonManager_SoundReceived;
        _applicationData.MicrophoneCaptureManager.OnCaptureStarted += MicrophonManager_CaptureStarted;
        _applicationData.MicrophoneCaptureManager.OnCaptureFinished += MicrophonManager_CaptureFinished;
        _comunicator.OnUser_SentAudioFrame += Comunicator_SoundReceived;
        _comunicator.OnUser_TurnedMicrophone_ON += Comunicator_UserTurnedMicrophoneOn;
        _comunicator.OnUser_TurnedMicrophone_OFF += Comunicator_UserTurnedMicrophoneOff;



        Task.Run(async() => await _comunicator.SEND_USER_JOINED_MEETING(CurrentUser.Id, _meetingId));
    }
    void ISeverEventSubsribable.Unsubscribe()
    {
        //participating
        //===========================================================================
        _comunicator.OnUser_JoinedMeeting -= Comunicator_OnUserJoinedMeeting;
        _comunicator.OnUser_LeftMeeting -= Comunicator_OnUserLeftMeeting;
        //camera frames
        //===========================================================================
        _comunicator.OnCameraFrameOfUserUpdated -= Comunicator_OnCameraFrameReceived;
        _comunicator.OnUser_TurnedCamera_OFF -= Comunicator_OnCameraTurnedOff;
        _comunicator.OnUser_TurnedCamera_ON -= Comunicator_OnCameraTurnedOn;
        _webCamera.OnError -= OnErrorReceived;
        _webCamera.OnCaptureStarted -= WebCameraManager_OnCaptureStarted;
        _webCamera.OnCaptureFinished -= WebCameraManager_OnCaptureFinished;
        _webCamera.OnImageCaptured -= WebCameraManager_OnFrameCaptured;
        //screen capture
        //===========================================================================
        _comunicator.OnScreenDemonstrationFrameOfUserUpdated -= Comunicator_OnScreenFrameReceived;
        _comunicator.OnUser_TurnedDemonstration_ON -= Comunicator_OnScreenDemonstrationStarted;
        _comunicator.OnUser_TurnedDemonstration_OFF -= Comunicator_OnScreenDemonstrationFinished;
        _screenCaptureManager.OnError -= OnErrorReceived;
        _screenCaptureManager.OnCaptureStarted -= ScreenCaptureManager_OnCaptureStarted;
        _screenCaptureManager.OnCaptureFinished -= ScreenCaptureManager_OnCaptureFinished;
        _screenCaptureManager.OnImageCaptured -= ScreenCaptureManager_OnFrameCaptured;
        //messages
        //===========================================================================
        _comunicator.OnMessageSent -= Comunicator_OnMessageReceived;
        _comunicator.OnErrorReceived -= OnErrorReceived;
        _comunicator.OnSuccessReceived -= OnSuccessReceived;
        _applicationData.ThemeManager.OnThemeChanged -= NotifyThemeChanged;
        //audio
        //===========================================================================
        _applicationData.MicrophoneCaptureManager.OnSoundCaptured -= MicrophonManager_SoundReceived;
        _applicationData.MicrophoneCaptureManager.OnCaptureStarted -= MicrophonManager_CaptureStarted;
        _applicationData.MicrophoneCaptureManager.OnCaptureFinished -= MicrophonManager_CaptureFinished;
        _comunicator.OnUser_SentAudioFrame -= Comunicator_SoundReceived;
        _comunicator.OnUser_TurnedMicrophone_ON -= Comunicator_UserTurnedMicrophoneOn;
        _comunicator.OnUser_TurnedMicrophone_OFF -= Comunicator_UserTurnedMicrophoneOff;
    }
    public void Dispose()
    {
        var vm = this as ISeverEventSubsribable;
        _webCamera.StopCapturing();
        _screenCaptureManager.StopCapturing();

        vm.Unsubscribe();


        Task.Run(async () => await _applicationData.Comunicator.SEND_USER_LEAVE_MEETING(CurrentUser.Id, CurrentUser.Username));
    }
    #endregion
}
#pragma warning restore CS8618