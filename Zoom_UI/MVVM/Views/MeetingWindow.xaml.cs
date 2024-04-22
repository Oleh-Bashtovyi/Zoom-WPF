using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WebEye.Controls.Wpf;
using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI.MVVM.Views;


public partial class MeetingWindow : Window
{
    public MeetingWindow()
    {
        InitializeComponent();


        var _webCameraControl = new WebCameraControl();
/*        var _webCameraId = _webCameraControl.GetVideoCaptureDevices().ElementAt(1);
        _webCameraControl.StartCapture(_webCameraId);*/


        DataContext = new MeetingViewModel(_webCameraControl);
    }
}
