using System.Collections.ObjectModel;
using System.Windows;
using WebEye.Controls.Wpf;
using Zoom_Server.Logging;
using Zoom_UI.ClientServer;
using Zoom_UI.Managers;
using Zoom_UI.Managersl;
using Zoom_UI.MVVM.Core;
using Zoom_UI.MVVM.Models;
using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI;


public partial class App : Application
{
    private readonly UdpComunicator comunicator;
    private readonly ViewModelNavigator viewModelNavigator;
    private readonly WebCameraControl webCamera;
    private readonly ThemeManager themeManager;
    private readonly UserViewModel currentUser;
    private readonly ApplicationData applicationData;
    private readonly WebCameraCaptureManager cameraCaptureManager;
    private readonly ScreenCaptureManager screenCaptureManager;
    private readonly MicrophoneCaptureManager microphoneCaptureManager;
    private readonly ObservableCollection<DebugMessage> ErrorsBuffer = new();
    private readonly LoggerWithCollection ErrorLoger;

    private int _serverPort = 9999;
    private string _serverIP = "127.0.0.1";

    public App()
    {
        ErrorLoger = new(ErrorsBuffer);
        comunicator = new(_serverIP, _serverPort, ErrorLoger);
        viewModelNavigator = new ViewModelNavigator();
        webCamera = new WebCameraControl();
        themeManager = new ThemeManager();
        currentUser = new UserViewModel();
        cameraCaptureManager = new WebCameraCaptureManager(webCamera);
        screenCaptureManager = new ScreenCaptureManager(1080, 1920, 3);
        currentUser.IsCurrentUser = true;
        microphoneCaptureManager = new();
        applicationData = new(
            comunicator, 
            cameraCaptureManager, 
            themeManager, currentUser, 
            viewModelNavigator, 
            screenCaptureManager,
            microphoneCaptureManager,
            ErrorLoger);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainViewModel = new MainViewModel(applicationData);
        viewModelNavigator.CurrentViewModel = new HomeViewModel(applicationData);
        //viewModelNavigator.CurrentViewModel = new MeetingViewModel(applicationData, new(1));
        currentUser.Username = "TEMP USERNAME~~~";

        MainWindow = new MainWindow()
        {
            DataContext = mainViewModel
        };

        MainWindow.Show();
        comunicator.Run();
        base.OnStartup(e);
    }
}
