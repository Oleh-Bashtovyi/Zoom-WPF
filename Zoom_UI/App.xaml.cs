using NAudio.Wave;
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
    private readonly ZoomClient comunicator;
    private readonly ViewModelNavigator viewModelNavigator;
    private readonly WebCameraControl webCamera;
    private readonly ThemeManager themeManager;
    private readonly ApplicationData applicationData;
    private readonly WebCameraCaptureManager cameraCaptureManager;
    private readonly ScreenCaptureManager screenCaptureManager;
    private readonly MicrophoneCaptureManager microphoneCaptureManager;
    private readonly ObservableCollection<DebugMessage> ErrorsBuffer = new();
    private readonly LoggerWithCollection ErrorLoger;
    private readonly WaveFormat waveFormat;
    private readonly AudioManager audioManager;

    private int _serverPort = 9999;
    private string _serverIP = "127.0.0.1";

    public App()
    {
        waveFormat = new WaveFormat(44100, 16, 1);
        audioManager = new(waveFormat);
        ErrorLoger = new(ErrorsBuffer);
        comunicator = new(_serverIP, _serverPort, ErrorLoger, TimeSpan.FromSeconds(20));
        viewModelNavigator = new ViewModelNavigator();
        webCamera = new WebCameraControl();
        themeManager = new ThemeManager();
        cameraCaptureManager = new WebCameraCaptureManager(webCamera);
        screenCaptureManager = new ScreenCaptureManager(1080, 1920, 10);
        microphoneCaptureManager = new(waveFormat);
        applicationData = new(
            comunicator, 
            cameraCaptureManager, 
            themeManager, 
            viewModelNavigator, 
            screenCaptureManager,
            microphoneCaptureManager,
            ErrorLoger,
            audioManager
            );
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainViewModel = new MainViewModel(applicationData);
        viewModelNavigator.CurrentViewModel = new HomeViewModel(applicationData);
        //viewModelNavigator.CurrentViewModel = new MeetingViewModel(applicationData, new(1, new("fsf", 1)));

        MainWindow = new MainWindow()
        {
            DataContext = mainViewModel
        };

        MainWindow.Show();
        comunicator.Run();
        audioManager.Play();
        base.OnStartup(e);
        //this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }






    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.Handled = true;
        ShutdownApplication();
    }

    private void HandleException(Exception exception)
    {
        MessageBox.Show($"An unexpected error occurred: {exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ShutdownApplication()
    {
        Current.Shutdown();
    }
}
