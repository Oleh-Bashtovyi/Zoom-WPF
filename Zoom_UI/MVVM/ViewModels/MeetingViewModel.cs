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
using System.IO;
using Zoom_UI.Managers;
using NAudio.Wave;
using Zoom_Server.Net.Codes;
using Zoom_UI.MVVM.Models.Frames;
namespace Zoom_UI.MVVM.ViewModels;
#pragma warning disable CS8618

public class MeetingViewModel : ViewModelBase, ISeverEventSubsribable, IDisposable
{
    private readonly UserViewModel _everyone = new("Everyone", -1);
    private UserViewModel _selectedParticipant;
    private UserViewModel _currentUser;
    private ViewModelNavigator _navigator;
    private ZoomClient _comunicator;
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
    private WaveOut _waveOut = new();
    private BufferedWaveProvider waveProvider;

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
    public UserViewModel? ScreenDemonstrator
    {
        get => _screenDemonstrator;
        set => SetAndNotifyPropertyChanged(ref _screenDemonstrator, value);
    }
    public BitmapImage? ScreenDemonstrationImage
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
        SwitchCameraStateCommand =    new RelayCommand(Switch_CameraCaptureState);
        SwitchMicrophonStateCommand = new RelayCommand(Switch_MicrophoneState);
        LeaveMeetingCommand =         new RelayCommand(LeaveMeeting);
        StartSharingScreenCommand =   new RelayCommand(Switch_ScreenCaptureState);
        ChangeThemeCommand =          new RelayCommand(Switch_Theme);
        CopyMeetingIdCommand =        new RelayCommand(
            () => Clipboard.SetText(MeetingId.ToString()), 
            () => !string.IsNullOrWhiteSpace(MeetingId.ToString()));
        SendMessageCommand =          new RelayCommand(
            () => SendMessage(), 
            () => !string.IsNullOrWhiteSpace(Message) && SelectedParticipant != null);
        #endregion

        #region Initial_data
        _applicationData = data;
        ParticipantsSelection.Add(_everyone);
        Participants.Add(meeting.CurrentUser);
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

        ScreenDemonstrator = meeting.ScreenDemonstrator;
        if(ScreenDemonstrator != null)
        {
            IsDemonstrationActive = true;
        }
        CurrentUser = meeting.CurrentUser;

        foreach(var participant in meeting.Participants)
        {
            AddUserToMeeting(participant);
        }
    }











    #region Buttons:_Microphone_Camera_ScreenCapture_Theme
    private void Switch_MicrophoneState()
    {
        try
        {
            if (_applicationData.MicrophoneCaptureManager.IsMicrophonTurnedOn)
            {
                _applicationData.MicrophoneCaptureManager.StopRecording();
            }
            else if (SellectedAudioDeviceIndex >= 0)
            {
                _applicationData.MicrophoneCaptureManager.StartRecording(SellectedAudioDeviceIndex);
            }
        }
        catch (Exception ex)
        {
            ShowAndLogError(ex.Message);
        }
    }
    private void Switch_CameraCaptureState()
    {
        try
        {
            if (CurrentUser.IsCameraOn)
            {
                _webCamera.StopCapturing();
            }
            else
            {
                _webCamera.StartCapturing(SelectedWebCamDevice);
            }
        }
        catch (Exception ex)
        {
            ShowAndLogError(ex.Message);
        }
    }
    private void Switch_ScreenCaptureState()
    {
        if (IsDemonstrationActive && ScreenDemonstrator != null && ScreenDemonstrator.Id == CurrentUser.Id)
        {
            _screenCaptureManager.StopCapturing();
        }
        else
        {
            _comunicator.Send_UserTurnScreenCapture_On(CurrentUser.Id, MeetingId);
        }
    }
    private void Switch_Theme()
    {
        try
        {
            _applicationData.ThemeManager.NextTheme();
        }
        catch (Exception ex)
        {
            ShowAndLogError(ex.Message);
        }
    }
    private void ShowAndLogError(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ErrorsList.Add(new(message));
            MessageBox.Show(message);
        });
    }
    private void LeaveMeeting()
    {
        _comunicator.Send_UserLeftMeeting(CurrentUser.Id, MeetingId);
    }
    private void DownloadFile(FileModel file)
    {
        var openFolderDialog = new Microsoft.Win32.OpenFolderDialog();

        openFolderDialog.Title = "Select path to save file";

        if (openFolderDialog.ShowDialog() ?? false)
        {
            var savePath = System.IO.Path.Combine(openFolderDialog.FolderName, file.FileName);

        }
    }
    private void SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(Message))
        {
            var selectedUserId = SelectedParticipant?.Id ?? -1;

            if (selectedUserId <= 0)
            {
                _comunicator.Send_MessageEveryone(CurrentUser.Id, MeetingId, Message);
                Message = string.Empty;
            }
            else
            {
                _comunicator.Send_Message(CurrentUser.Id, selectedUserId, MeetingId, Message);
                Message = string.Empty;
            }
        }
    }
    private void SendFile()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog();

        openFileDialog.Title = "Select file to send";

        if (openFileDialog.ShowDialog() ?? false)
        {

            if (openFileDialog.FileName != null)
            {

                var file = new FileInfo(openFileDialog.FileName);

                if (file.Length < 128 * 1024 * 1024)
                {
                    var receiver = SelectedParticipant == _everyone ? null : SelectedParticipant;
                    var sender = CurrentUser;
                    var message = new MessageModel(sender.Username, receiver?.Username ?? "Everyone", DateTime.Now, null);
                    var cfi = new ChatFileItem(file, sender, receiver, message, _comunicator, message, MeetingId);
                    message.Content = cfi;
                    cfi.Deleted += OnFileDeleted;
                    cfi.Uploaded += OnFileUploaded;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ParticipantsMessages.Add(message);
                    });
                }
                else
                {
                    MessageBox.Show("Max file size - 128 MB", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
    #endregion












    private void OnFileUploaded(ChatFileItem chatFileItem)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            chatFileItem.Uploaded -= OnFileUploaded;
            chatFileItem.Deleted -= OnFileDeleted;
            var mes = chatFileItem.Wraper;

            for (var i = 0; i < ParticipantsMessages.Count; i++)
            {
                if (ParticipantsMessages[i] == mes)
                {
                    ParticipantsMessages.RemoveAt(i);

                    var downloadChatFile = new ChatFileItemDownload(
                        chatFileItem.FileSize, 
                        chatFileItem.FileName, 
                        chatFileItem.FileId, 
                        MeetingId, 
                        _comunicator);
                    var newMessage = new MessageModel();
                    newMessage.From = mes.From;
                    newMessage.To = mes.To;
                    newMessage.When = mes.When;
                    newMessage.Content = downloadChatFile;
                    ParticipantsMessages.Add(newMessage);
                    break;
                }
            }
        });
    }








    private void OnFileDeleted(ChatFileItem message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ParticipantsMessages.Remove(message.Wraper);
        });
    }





    private void AddNewMessage(string sender, string receiver, object content)
    {
        var message = new MessageModel();
        message.Content = content;
        message.When = DateTime.Now;
        message.From = sender;
        message.To = receiver;
        ParticipantsMessages.Add(message);
    }


    private void AddUserToMeeting(UserViewModel model)
    {
        var existingUser = Participants.FirstOrDefault(x => x.Id == model.Id);

        if(existingUser != null)
        {
            existingUser.Username = model.Username; 
        }
        else
        {
            Participants.Add(model);

            if (model.Id != CurrentUser.Id)
            {
                ParticipantsSelection.Add(model);
            }
        }
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
            Participants.Remove(existingUser);
            ParticipantsSelection.Remove(existingUser);

            if (ScreenDemonstrator?.Id == model.Id)
            {
                ScreenDemonstrator = null;
                ScreenDemonstrationImage = null;
                IsDemonstrationActive = false;
            }
        }
    }







    #region SERVER_EVENTS

    #region PARTICIPATING_EVENTS
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
                ((ISeverEventSubsribable)this).UnsubscribeEvents();
            }
            else 
            {
                RemoveUserFromMeeting(model);
            }
        });
    }
    #endregion

    #region CAMERA_EVENTS
    private void Comunicator_OnCameraFrameReceived(ImageFrame frame)
    {
        if(frame.UserId != CurrentUser.Id)
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
        _comunicator.Send_UserTurnCamera_On(CurrentUser.Id, MeetingId);
    }
    private void WebCameraManager_OnCaptureFinished()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentUser.IsCameraOn = false;
        });
        _comunicator.Send_UserTurnCamera_Off(CurrentUser.Id, MeetingId);
    }
    private void WebCameraManager_OnFrameCaptured(Bitmap? bitmap)
    {
        if(bitmap != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentUser.CameraImage = bitmap.AsBitmapImage();
            });
            _comunicator.Send_CameraFrame(CurrentUser.Id, MeetingId, bitmap.ResizeBitmap(250, 250));
        }
    }
    #endregion

    #region SCREEN_EVENTS
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
        //MessageBox.Show($"Demonstrator id: {frame.UserId}, Current user: {CurrentUser.Id}");

        if(frame.UserId != CurrentUser.Id)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ScreenDemonstrationImage = frame.Image;
            });
        }
    }
    private void ScreenCaptureManager_OnCaptureStarted()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            //ErrorsList.Add(new("Screen capture manager STARTED capture process!"));
            ScreenDemonstrator = CurrentUser;
            IsDemonstrationActive = true;
        });
    }
    private void ScreenCaptureManager_OnCaptureFinished()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            //ErrorsList.Add(new("Screen capture manager FINISHED capture process!"));
            ScreenDemonstrator = null;
            IsDemonstrationActive = false;
            _comunicator.Send_UserTurnScreenCapture_Off(CurrentUser.Id, MeetingId);
        });
    }
    private void ScreenCaptureManager_OnFrameCaptured(Bitmap? bitmap)
    {
        if(bitmap != null)
        {
            ScreenDemonstrationImage = bitmap.AsBitmapImage();
            _comunicator.Send_ScreenFrame(CurrentUser.Id, MeetingId, bitmap.ResizeBitmap(1000, 1000));
        }
    }
    #endregion

    #region MESSAGE_EVENTS
    private void Comunicator_OnMessageReceived(MessageInfo message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddNewMessage(message.Sender, message.Receiver, message?.Content ?? string.Empty);
        });
    }
    private void Comunicator_OnErrorReceived(ErrorModel model)
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

        ShowAndLogError(model?.Message ?? string.Empty);
    }
    private void Comunicator_OnSuccessReceived(SuccessModel model)
    {
        if (model.SuccessCode == ScsCode.SCREEN_DEMONSTRATION_ALLOWED)
        {
            //ErrorsList.Add(new("Server allowed to capture screen"));
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
    #endregion

    #region AUDIO_EVENTS
    private void MicrophonManager_CaptureStarted()
    {
        CurrentUser.IsMicrophoneOn = true;
        _comunicator.Send_UserTurnMicrophone_On(CurrentUser.Id, MeetingId);
    }
    private void MicrophonManager_SoundReceived(byte[] soundBytes)
    {
        //waveProvider.AddSamples(soundBytes, 0, soundBytes.Length);
        _comunicator.Send_Audio(CurrentUser.Id, MeetingId, soundBytes);
    }

    private void MicrophonManager_CaptureFinished()
    {
        CurrentUser.IsMicrophoneOn = false;
        _comunicator.Send_UserTurnMicrophone_Off(CurrentUser.Id, MeetingId);
    }
    private void Comunicator_UserTurnedMicrophoneOn(UserModel model)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var user = Participants.FirstOrDefault(x => x.Id == model.Id);

            if(user != null)
            {
                user.IsMicrophoneOn = true;

                ErrorsList.Add(new($"(VIEW_MODEL): User {model.Id} found, micro turn on"));
            }
            else ErrorsList.Add(new($"(VIEW_MODEL): User with such id: {model.Id} not found for picrophon turning on"));
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

                ErrorsList.Add(new($"(VIEW_MODEL): User {model.Id} found, micro turn off"));
            }
            else ErrorsList.Add(new($"(VIEW_MODEL): User with such id: {model.Id} not found for picrophon turning off"));
        });
    }
    private void Comunicator_SoundReceived(AudioFrame audioFrame)
    {
/*        if(audioFrame.UserId == CurrentUser.Id)
        {
            return;
        }*/

        Application.Current.Dispatcher.Invoke(() =>
        {
            //ErrorsList.Add(new($"(VIEW_MODEL): Sound received! User: {audioFrame.UserId} Size: {audioFrame.Data.Length}"));
            waveProvider.AddSamples(audioFrame.Data, 0, audioFrame.Data.Length);
        });
    }
    #endregion







    //EVENTS SUBSCRIPTION
    //===========================================================
    private void NotifyThemeChanged(string newTheme) => OnPropertyChanged(nameof(CurrentTheme));
    void ISeverEventSubsribable.SubscribeEvents()
    {
        //participating
        //===========================================================================
        _comunicator.OnUser_JoinedMeeting += Comunicator_OnUserJoinedMeeting;
        _comunicator.OnUser_LeftMeeting += Comunicator_OnUserLeftMeeting;
        //camera frames
        //===========================================================================
        _comunicator.OnCameraFrameReceived += Comunicator_OnCameraFrameReceived;
        _comunicator.OnUser_TurnedCamera_OFF += Comunicator_OnCameraTurnedOff;
        _comunicator.OnUser_TurnedCamera_ON += Comunicator_OnCameraTurnedOn;
        _webCamera.OnError += Comunicator_OnErrorReceived;
        _webCamera.OnCaptureStarted += WebCameraManager_OnCaptureStarted;
        _webCamera.OnCaptureFinished += WebCameraManager_OnCaptureFinished;
        _webCamera.OnImageCaptured += WebCameraManager_OnFrameCaptured;
        //screen capture
        //===========================================================================
        _comunicator.OnScreenCaptureFrameReceived += Comunicator_OnScreenFrameReceived;
        _comunicator.OnUser_TurnedDemonstration_ON += Comunicator_OnScreenDemonstrationStarted;
        _comunicator.OnUser_TurnedDemonstration_OFF += Comunicator_OnScreenDemonstrationFinished;
        _screenCaptureManager.OnError += Comunicator_OnErrorReceived;
        _screenCaptureManager.OnCaptureStarted += ScreenCaptureManager_OnCaptureStarted;
        _screenCaptureManager.OnCaptureFinished += ScreenCaptureManager_OnCaptureFinished;
        _screenCaptureManager.OnImageCaptured += ScreenCaptureManager_OnFrameCaptured;
        //messages
        //===========================================================================
        _comunicator.OnMessageSent += Comunicator_OnMessageReceived;
        _comunicator.OnErrorReceived += Comunicator_OnErrorReceived;
        _comunicator.OnSuccessReceived += Comunicator_OnSuccessReceived;
        _comunicator.OnFileUploaded += _comunicator_OnFileUploaded;
        _applicationData.ThemeManager.OnThemeChanged += NotifyThemeChanged;
        //audio
        //===========================================================================
        _applicationData.MicrophoneCaptureManager.OnSoundCaptured += MicrophonManager_SoundReceived;
        _applicationData.MicrophoneCaptureManager.OnCaptureStarted += MicrophonManager_CaptureStarted;
        _applicationData.MicrophoneCaptureManager.OnCaptureFinished += MicrophonManager_CaptureFinished;
        _comunicator.OnUser_SentAudioFrame += Comunicator_SoundReceived;
        _comunicator.OnUser_TurnedMicrophone_ON += Comunicator_UserTurnedMicrophoneOn;
        _comunicator.OnUser_TurnedMicrophone_OFF += Comunicator_UserTurnedMicrophoneOff;
    }

    private void _comunicator_OnFileUploaded(MessageInfo obj)
    {
        object? content = null;

        if(obj.Content is FileModel fileModel)
        {
            content = new ChatFileItemDownload(fileModel.FileSize, fileModel.FileName, fileModel.Id, MeetingId, _comunicator);
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            AddNewMessage(obj.Sender, obj.Receiver, content ?? string.Empty);
        });
    }

    void ISeverEventSubsribable.UnsubscribeEvents()
    {
        //participating
        //===========================================================================
        _comunicator.OnUser_JoinedMeeting -= Comunicator_OnUserJoinedMeeting;
        _comunicator.OnUser_LeftMeeting -= Comunicator_OnUserLeftMeeting;
        //file
        //===========================================================================
        _comunicator.OnFileUploaded -= _comunicator_OnFileUploaded;
        //camera frames
        //===========================================================================
        _comunicator.OnCameraFrameReceived -= Comunicator_OnCameraFrameReceived;
        _comunicator.OnUser_TurnedCamera_OFF -= Comunicator_OnCameraTurnedOff;
        _comunicator.OnUser_TurnedCamera_ON -= Comunicator_OnCameraTurnedOn;
        _webCamera.OnError -= Comunicator_OnErrorReceived;
        _webCamera.OnCaptureStarted -= WebCameraManager_OnCaptureStarted;
        _webCamera.OnCaptureFinished -= WebCameraManager_OnCaptureFinished;
        _webCamera.OnImageCaptured -= WebCameraManager_OnFrameCaptured;
        //screen capture
        //===========================================================================
        _comunicator.OnScreenCaptureFrameReceived -= Comunicator_OnScreenFrameReceived;
        _comunicator.OnUser_TurnedDemonstration_ON -= Comunicator_OnScreenDemonstrationStarted;
        _comunicator.OnUser_TurnedDemonstration_OFF -= Comunicator_OnScreenDemonstrationFinished;
        _screenCaptureManager.OnError -= Comunicator_OnErrorReceived;
        _screenCaptureManager.OnCaptureStarted -= ScreenCaptureManager_OnCaptureStarted;
        _screenCaptureManager.OnCaptureFinished -= ScreenCaptureManager_OnCaptureFinished;
        _screenCaptureManager.OnImageCaptured -= ScreenCaptureManager_OnFrameCaptured;
        //messages
        //===========================================================================
        _comunicator.OnMessageSent -= Comunicator_OnMessageReceived;
        _comunicator.OnErrorReceived -= Comunicator_OnErrorReceived;
        _comunicator.OnSuccessReceived -= Comunicator_OnSuccessReceived;
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

        vm.UnsubscribeEvents();

        _applicationData.Comunicator.Send_UserLeftMeeting(CurrentUser.Id, MeetingId);
    }
    #endregion
}
#pragma warning restore CS8618