using System.Collections.ObjectModel;
using Zoom_Server.Logging;
using Zoom_UI.ClientServer;
using Zoom_UI.Managers;
using Zoom_UI.Managersl;
using Zoom_UI.MVVM.Models;
namespace Zoom_UI.MVVM.Core;

public class ApplicationData
{
    public ZoomClient ZoomClient { get; }
    public WebCameraCaptureManager WebCameraCaptureManager {  get; }
    public MicrophoneCaptureManager MicrophoneCaptureManager { get; }
    public ScreenCaptureManager ScreenCaptureManager { get; }
    public ThemeManager ThemeChangeManager { get; }
    public AudioManager AudioManager { get; }
    public ViewModelNavigator PagesNavigator { get; }
    public ObservableCollection<DebugMessage> ErrorsBuffer { get; }
    public LoggerWithCollection LoggerWithCollection { get; }
    public ApplicationData(
        ZoomClient comunicator,
        WebCameraCaptureManager webCamera, 
        ThemeManager themeManager, 
        ViewModelNavigator navigator,
        ScreenCaptureManager screenCaptureManager,
        MicrophoneCaptureManager microphoneCaptureManager,
        LoggerWithCollection logger,
        AudioManager audioManager)
    {
        ZoomClient = comunicator;
        WebCameraCaptureManager = webCamera;
        ThemeChangeManager = themeManager;
        PagesNavigator = navigator;
        ScreenCaptureManager = screenCaptureManager;
        MicrophoneCaptureManager = microphoneCaptureManager;
        LoggerWithCollection = logger;
        ErrorsBuffer = logger.GetBuffer();
        AudioManager = audioManager;
    }
}
