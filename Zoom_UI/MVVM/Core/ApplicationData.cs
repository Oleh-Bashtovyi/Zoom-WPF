using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEye.Controls.Wpf;
using Zoom_UI.ClientServer;
using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI.MVVM.Core;

public class ApplicationData
{
    public UdpComunicator Comunicator { get; }
    public WebCameraControl WebCamera {  get; }
    public ThemeManager ThemeManager { get; }
    public UserViewModel CurrentUser {  get; }
    public ViewModelNavigator Navigator { get; }

    public ApplicationData(
        UdpComunicator comunicator, 
        WebCameraControl webCamera, 
        ThemeManager themeManager, 
        UserViewModel currentUser, 
        ViewModelNavigator navigator)
    {
        Comunicator = comunicator;
        WebCamera = webCamera;
        ThemeManager = themeManager;
        CurrentUser = currentUser;
        Navigator = navigator;
    }
}
