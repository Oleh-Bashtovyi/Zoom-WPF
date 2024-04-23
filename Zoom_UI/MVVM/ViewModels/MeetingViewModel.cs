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
namespace Zoom_UI.MVVM.ViewModels;
#pragma warning disable CS8618

public class MeetingViewModel : ViewModelBase
{
    private readonly UserViewModel _everyone = new("Everyone", "Everyone");
    private UserViewModel _selectedParticipant;
    private string _message;
    private string _theme;


    public string MeetingId {  get;  }
    public string CurrentTheme
    {
        get => _theme;
        set => SetAndNotifyPropertyChanged(ref _theme, value);
    }
    public UserViewModel CurrentUser {  get; private set; }
    public ObservableCollection<string> ErrorsList { get; private set; } = new();
    public ObservableCollection<UserViewModel> Participants { get; } = new();
    public ObservableCollection<UserViewModel> ParticipantsSelection { get; } = new();
    public ObservableCollection<MessageModel> ParticipantsMessages { get; } = new();


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


    public string CurrentMicrophoneColorKey
    {
        //get { return CurrentUser.IsMicrophoneOn ? "OnButton" : "OnButton"; }
        get 
        { 
            return CurrentUser.IsMicrophoneOn ? "OffButton" : "OffButton"; 
        }
    }


    public ICommand SendMessageCommand { get; }
    public ICommand CopyMeetingIdCommand { get; }
    public ICommand SwitchMicrophonStateCommand { get; }
    public ICommand SwitchCameraStateCommand { get; }
    public ICommand LeaveMeetingCommand {  get; }
    public ICommand ChangeThemeCommand {  get; }





    WebCameraControl? _webCameraControl;

    public MeetingViewModel(WebCameraControl webCameraControl)

    {
        MeetingId = "12345";

        CopyMeetingIdCommand = new RelayCommand(() => Clipboard.SetText(MeetingId), () => !string.IsNullOrWhiteSpace(MeetingId));
        SendMessageCommand = new RelayCommand(SendMessage, () => !string.IsNullOrWhiteSpace(Message));
        SwitchCameraStateCommand = new RelayCommand(SwitchCameraState, null);
        SwitchMicrophonStateCommand = new RelayCommand(SwitchMicrophoneState, null);



        _webCameraControl = webCameraControl;
        ParticipantsSelection.Add(_everyone);
        CurrentTheme = "light";

        ChangeThemeCommand = new RelayCommand(() =>
        {
            try
            {
                string serverIP = "127.0.0.1";
                int serverPort = 9999;
/*                UdpClient udpClient = new UdpClient();
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);*/

                TcpClient client = new TcpClient();
                client.Connect(serverIP, serverPort);

/*                using var client = new TcpClient(serverEndPoint);*/

                var stream = client.GetStream();

                stream.Write([1, 2, 3, 4, 5], 0, 5);
                stream.Flush();


                //==============================================================================================
                //THIS WORKS
                //==============================================================================================
/*                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                bw.Write((byte)OpCode.Success);

                //bw.Write("Hello world");

                bw.Write((new Bitmap("E:\\work\\course_2\\semester_2\\ookp\\Lab_7\\Zoom\\Zoom_UI\\Assets\\copy.png")).ToByteArray());



                AddNewMessage(_everyone, _everyone, $"Data sent! Byte array: [{string.Join(",", ms.ToArray())}]");

                udpClient.Send(ms.ToArray(), (int)ms.Length, serverEndPoint);

                AddNewMessage(_everyone, _everyone, "Data sent using UDP!");
                udpClient.Dispose();*/
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                ErrorsList.Add(ex.Message);
            }

            /*            if(CurrentTheme == "light")
                        {
                            CurrentTheme = "dark";
                            ReplaceTheme("DarkTheme");
                        }
                        else
                        {
                            CurrentTheme = "light";
                            ReplaceTheme("LightTheme");
                        }*/
        });


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

        ErrorsList.Add("Some error");
        ErrorsList.Add("Some error 2");
        ErrorsList.Add("Some very biiiig error that IDK. and dfffgfgggdg..... difjsdgojdskglj g jdgskdg ;gdsgoigjgsdpgjigoj");
        ErrorsList.Add("Some error 2");
        ErrorsList.Add("Some error 2");
        ErrorsList.Add("Some error 2");
        ErrorsList.Add("Some error 2");
        CurrentUser = Participants[1];

        AddNewMessage(Participants[0], _everyone, "Hello everyone!");
        AddNewMessage(Participants[0], Participants[0], "Hello this must be visible only to one user!");


        SwitchMicrophonStateCommand = new RelayCommand(() => 
        {
            CurrentUser.IsMicrophoneOn = !CurrentUser.IsMicrophoneOn;
            Message += "AB";
        });

        SwitchCameraStateCommand = new RelayCommand(() =>
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

        CurrentUser.IsMicrophoneOn = true;
        Message = "THIIIIISS jfdlkjgdkslgn klsdngkdglskgnsk s  sj d jgskgjds gj h sdhgkldshgkghkhs  k sdklgdslhglsdlghg hsh lgdsglgsg";
        SelectedParticipant = _everyone;
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





    private void SendMessage()
    {
        
    }

    private void SwitchMicrophoneState()
    {

    }


    private void SwitchCameraState()
    {

    }

    private void ReplaceTheme(string newTheme)
    {
        var newThemeDict = new ResourceDictionary()
        {
            Source = new Uri($"Themes/{newTheme}.xaml", UriKind.Relative)
        };


        ResourceDictionary oldTheme = 
            Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString == "Themes/LightTheme.xaml");

        if (oldTheme != null)
        {
            // Remove the old ResourceDictionary
            Application.Current.Resources.MergedDictionaries.Remove(oldTheme);
        }

        // Add the new ResourceDictionary
        Application.Current.Resources.MergedDictionaries.Add(newThemeDict);





        /*        ResourceDictionary? themeDictionary = null;

                foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
                {
                    if (dictionary is ResourceDictionary)
                    {
                        var resourceDict = dictionary as ResourceDictionary;
                        if (resourceDict.Contains("ThemeResourceDictionary"))
                        {
                            themeDictionary = resourceDict;
                            break;
                        }
                    }
                }

                if (themeDictionary != null)
                {
                    themeDictionary["ThemeResourceDictionary"] = new ResourceDictionary()
                    {
                        Source = new Uri($"Themes/{newTheme}.xaml", UriKind.Relative)
                    };
                }
        */




        /*        int themeIndex = -1;
                for (int i = 0; i < Application.Current.Resources.MergedDictionaries.Count; i++)
                {
                    if (Application.Current.Resources.MergedDictionaries[i].Source != null &&
                        Application.Current.Resources.MergedDictionaries[i].Source.ToString().Contains("ThemeResourceDictionary"))
                    {
                        themeIndex = i;
                        break;
                    }
                }

                // If the "ThemeResources" dictionary is found, replace it with the "DarkTheme" dictionary
                if (themeIndex != -1)
                {
                    Application.Current.Resources.MergedDictionaries.RemoveAt(themeIndex);
                    Application.Current.Resources.MergedDictionaries.Insert(themeIndex, new ResourceDictionary()
                    {
                        Source = new Uri($"Themes/{newTheme}.xaml", UriKind.Relative)
                    });
                }*/
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

#pragma warning restore CS8618