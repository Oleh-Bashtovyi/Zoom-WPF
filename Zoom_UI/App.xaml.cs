using System.Configuration;
using System.Data;
using System.Windows;
using WebEye.Controls.Wpf;
using Zoom_Server.Logging;
using Zoom_UI.ClientServer;
using Zoom_UI.MVVM.Core;
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

    private int _serverPort = 9999;
    private string _serverIP = "127.0.0.1";

    public App()
    {
        comunicator = new(_serverIP, _serverPort, new LoggerWithConsole());
        viewModelNavigator = new ViewModelNavigator();
        webCamera = new WebCameraControl();
        themeManager = new ThemeManager();
        currentUser = new UserViewModel();
        currentUser.IsCurrentUser = true;
        applicationData = new(comunicator, webCamera, themeManager, currentUser, viewModelNavigator);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainViewModel = new MainViewModel(applicationData);
        viewModelNavigator.CurrentViewModel = new HomeViewModel(applicationData);
        //viewModelNavigator.CurrentViewModel = new MeetingViewModel(applicationData, new(1));

        MainWindow = new MainWindow()
        {
            DataContext = mainViewModel
        };

        MainWindow.Show();
        comunicator.Run();

        base.OnStartup(e);
    }
}
