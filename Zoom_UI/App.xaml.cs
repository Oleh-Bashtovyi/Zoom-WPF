using NAudio.Wave;
using System.Collections.ObjectModel;
using System.IO;
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
    private readonly ZoomClient zoomClient;
    private readonly ScreenRecordingManager screenRecordingManager;
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
        waveFormat = new(44100, 16, 1);
        microphoneCaptureManager = new(waveFormat);
        audioManager = new(waveFormat);
        ErrorLoger = new(ErrorsBuffer);
        zoomClient = new(_serverIP, _serverPort, ErrorLoger, TimeSpan.FromSeconds(20));
        viewModelNavigator = new();
        webCamera = new WebCameraControl();
        themeManager = new ThemeManager();
        cameraCaptureManager = new WebCameraCaptureManager(webCamera, 15);
        screenCaptureManager = new ScreenCaptureManager(1080, 1920, 10);
        screenRecordingManager = new ScreenRecordingManager();
        applicationData = new(
            zoomClient, 
            cameraCaptureManager, 
            themeManager, 
            viewModelNavigator, 
            screenCaptureManager,
            microphoneCaptureManager,
            ErrorLoger,
            audioManager,
            screenRecordingManager
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
        zoomClient.Start();
        audioManager.Start();
        base.OnStartup(e);
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        this.Exit += App_Exit;
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        zoomClient.Stop();
        cameraCaptureManager.StopCapturing();
        screenCaptureManager.StopCapturing();
        microphoneCaptureManager.StopRecording();
        screenRecordingManager.StopRecording();
        screenRecordingManager.Dispose();
        audioManager.Stop();
        audioManager.Dispose();
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.Handled = true;
        MessageBox.Show("ERROR!");
        ShutdownApplication();
    }

    private void HandleException(Exception exception)
    {
        const string errorsDirectory = "./APP_ERRORS";

        var path = $"{errorsDirectory}/ERROR - {DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.txt";

        Directory.CreateDirectory(errorsDirectory);
        File.WriteAllText(path, exception.ToString());

    }

    private void ShutdownApplication()
    {
        Current.Shutdown();
    }
}
