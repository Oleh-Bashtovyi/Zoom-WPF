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
    private readonly UdpComunicator listener;
    private readonly ViewModelNavigator viewModelNavigator;
    private readonly WebCameraControl webCamera;
    private int _serverPort = 9999;
    private string _serverIP = "127.0.0.1";

    public App()
    {
        listener = new(_serverIP, _serverPort, new LoggerWithConsole());
        viewModelNavigator = new ViewModelNavigator();
        webCamera = new WebCameraControl();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainViewModel = new MainViewModel(listener, viewModelNavigator);
        viewModelNavigator.CurrentViewModel = new HomeViewModel(listener, viewModelNavigator, webCamera);
        //viewModelNavigator.CurrentViewModel = new MeetingViewModel(listener, viewModelNavigator, new(), new(1), webCamera);

        MainWindow = new MainWindow()
        {
            DataContext = mainViewModel
        };

        MainWindow.Show();
        listener.Run();

        base.OnStartup(e);
    }
}
