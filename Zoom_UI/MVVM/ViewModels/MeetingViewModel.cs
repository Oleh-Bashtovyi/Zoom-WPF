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
namespace Zoom_UI.MVVM.ViewModels;
#pragma warning disable CS8618

public class MeetingViewModel : ViewModelBase
{
    private readonly UserViewModel _everyone = new("Everyone", -1);
    private readonly UserViewModel _none = new("", -1);
    private UserViewModel _selectedParticipant;
    private UserViewModel _currentUser;
    private UdpListener _listener;
    private string _message;
    private string _theme;
    private int _meetingId;
    private int _serverPort = 9999;
    private string _serverIP = "127.0.0.1";

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
        _listener.OnUserCreated +=         _listener_OnUserCreated;
        _listener.OnMeetingCreated +=      _listener_OnMeetingCreated;
        _listener.OnUserJoinedMeeting +=   _listener_OnUserJoinedMeeting;
        _listener.onUserLeftMeeting +=     _listener_onUserLeftMeeting;
        _listener.OnCameraFrameUpdated +=  _listener_OnCameraFrameUpdated;
        #endregion

        #region Initial_data
        ParticipantsSelection.Add(_everyone);
        SelectedParticipant = _everyone;
        CurrentTheme = "light";
        CurrentUser = new UserViewModel(string.Empty, 1);
        CurrentUser.IsMicrophoneOn = false;
        _udpClient = new UdpClient();
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        #endregion

        AddUserToCollections(CurrentUser);

        _webCameraControl = webCameraControl;
        ChangeThemeCommand = new RelayCommand(() =>
        {
            try
            {
                /*                string serverIP = "127.0.0.1";
                                int serverPort = 9999;
                *//*                UdpClient udpClient = new UdpClient();
                                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);*//*

                                TcpClient client = new TcpClient();
                                client.Connect(serverIP, serverPort);

                                *//*                using var client = new TcpClient(serverEndPoint);*//*

                                Task.Run(async () => 
                                {
                                    var stream = client.GetStream();

                                    //var data = new byte[]{ 1, 2, 3, 4, 5 };

                                    var data = (new Bitmap("E:\\work\\course_2\\semester_2\\ookp\\Lab_7\\Zoom\\Zoom_UI\\Assets\\copy.png")).ToByteArray();
                                    await stream.WriteAsync(data, 0, data.Length);
                                    await stream.FlushAsync();
                                });*/
                /*                var stream = client.GetStream();

                                stream.Write([1, 2, 3, 4, 5], 0, 5);
                                stream.Flush();*/


                //==============================================================================================
                //THIS WORKS
                //==============================================================================================
                /*                using var ms = new MemoryStream();
                                using var bw = new BinaryWriter(ms);
                                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
                                bw.Write((byte)OpCode.CreateMeeting);

                                //bw.Write("Hello world");

                               // bw.Write((new Bitmap("E:\\work\\course_2\\semester_2\\ookp\\Lab_7\\Zoom\\Zoom_UI\\Assets\\copy.png")).ToByteArray());



                                AddNewMessage(_everyone, _everyone, $"Data sent! Byte array: [{string.Join(",", ms.ToArray())}]");

                                udpClient.Send(ms.ToArray(), (int)ms.Length, serverEndPoint);

                                AddNewMessage(_everyone, _everyone, "Data sent using UDP!");*/
                //udpClient.Dispose();
                //Task.Run(AsyncSend);
                AddNewMessage(_everyone, _everyone, "Sending begun!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                ErrorsList.Add(ex.Message);
            }
        });
        //Task.Run(CaptureProcess);
        _listener.Run();
    }







    


    private UdpClient _udpClient { get; }


    private async Task AsyncSend()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIP), _serverPort);
        bw.Write((byte)OpCode.CreateMeeting);

        //AddNewMessage(_everyone, _everyone, $"Data sent! Byte array: [{string.Join(",", ms.ToArray())}]");

        await _udpClient.SendAsync(ms.ToArray(), (int)ms.Length, serverEndPoint);

        //AddNewMessage(_everyone, _everyone, "Data sent using UDP!");
    }


    private async Task ListeningProcess()
    {

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





    private void SendMessage()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentUser.Username = "AAAAAAA";
        });
    }

    private void SwitchMicrophoneState()
    {
        Task.Run(_listener.SomeOper);

/*        Task.Run(async () =>
        {
*//*            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIP), _serverPort);
            bw.Write((byte)OpCode.CreateUser);
            bw.Write("This is Username!");
            await _udpClient.SendAsync(ms.ToArray(), (int)ms.Length, serverEndPoint);*//*




            //AddNewMessage(_everyone, _everyone, $"Data sent! Byte array: [{string.Join(",", ms.ToArray())}]");

        });*/

/*        Application.Current.Dispatcher.Invoke(() =>
        {

        });*/
    }

    private void LeaveMeeting()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {

        });
    }

    private void SwitchCameraState()
    {
/*        if (_webCameraControl != null)
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
        }*/
    }







    #region Listener_events
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
    private void _listener_OnUserJoinedMeeting(UserModel obj)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddNewUser(obj.UID, obj.Username);
        });
    }
    private void _listener_OnUserCreated(UserModel obj)
    {

        Dispatcher.CurrentDispatcher.Invoke(() =>
        {
            //CurrentUser = obj.AsViewModel();
            CurrentUser.UID = obj.UID;
            CurrentUser.Username = obj.Username;
        });

/*        Task.Run(async () => await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CurrentUser = obj.AsViewModel();
        }));*/


        /*        Application.Current.Dispatcher.Invoke(() =>
                {
                });*/
    }
    private void _listener_OnMeetingCreated(MeetingInfo obj)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MeetingId = obj.Id;
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
    private void AddNewUser(int uid, string username)
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