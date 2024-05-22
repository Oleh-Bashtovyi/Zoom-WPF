using System.Collections.ObjectModel;
using Zoom_Server.Logging;
using Zoom_UI.ClientServer;
using Zoom_UI.Managers;
using Zoom_UI.Managersl;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.MVVM.Core;

public class ApplicationData
{
    public ZoomClient Comunicator { get; }
    public WebCameraCaptureManager WebCamera {  get; }
    public ScreenCaptureManager ScreenCaptureManager { get; }
    public ThemeManager ThemeManager { get; }
    public ViewModelNavigator Navigator { get; }
    public MicrophoneCaptureManager MicrophoneCaptureManager { get; }
    public ObservableCollection<DebugMessage> ErrorsBuffer { get; }
    public LoggerWithCollection LoggerWithCollection { get; }
    public AudioManager AudioManager { get; }

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
        Comunicator = comunicator;
        WebCamera = webCamera;
        ThemeManager = themeManager;
        Navigator = navigator;
        ScreenCaptureManager = screenCaptureManager;
        MicrophoneCaptureManager = microphoneCaptureManager;
        LoggerWithCollection = logger;
        ErrorsBuffer = logger.GetBuffer();
        AudioManager = audioManager;
    }
}
