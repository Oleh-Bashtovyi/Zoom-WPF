using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEye.Controls.Wpf;
using Zoom_UI.ClientServer;
using Zoom_UI.Managers;
using Zoom_UI.Managersl;
using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI.MVVM.Core;

public class ApplicationData
{
    public UdpComunicator Comunicator { get; }
    public WebCameraCaptureManager WebCamera {  get; }
    public ScreenCaptureManager ScreenCaptureManager { get; }
    public ThemeManager ThemeManager { get; }
    public UserViewModel CurrentUser {  get; }
    public ViewModelNavigator Navigator { get; }
    public MicrophoneCaptureManager MicrophoneCaptureManager { get; }

    public ApplicationData(
        UdpComunicator comunicator,
        WebCameraCaptureManager webCamera, 
        ThemeManager themeManager, 
        UserViewModel currentUser, 
        ViewModelNavigator navigator,
        ScreenCaptureManager screenCaptureManager,
        MicrophoneCaptureManager microphoneCaptureManager)
    {
        Comunicator = comunicator;
        WebCamera = webCamera;
        ThemeManager = themeManager;
        CurrentUser = currentUser;
        Navigator = navigator;
        ScreenCaptureManager = screenCaptureManager;
        MicrophoneCaptureManager = microphoneCaptureManager;
    }
}
