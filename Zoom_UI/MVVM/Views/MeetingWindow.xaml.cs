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

    private void MessagesSectionButton_Click(object sender, RoutedEventArgs e)
    {
        /*        if (MessagesContainer.Visibility == Visibility.Visible)
                {
                    MessagesContainer.Visibility = Visibility.Collapsed;
                    var column_definition = MainGrid.ColumnDefinitions.Last();
                    column_definition.MinWidth = 1;
                    GridSpliter.IsEnabled = false;
                }
                else
                {
                    MessagesContainer.Visibility = Visibility.Visible;
                    var column_definition = MainGrid.ColumnDefinitions.Last();
                    column_definition.MinWidth = 250;
                    GridSpliter.IsEnabled = true;
                }*/

        if (MessagesContainer.Visibility == Visibility.Visible)
        {
            MessagesContainer.Visibility = Visibility.Collapsed;
            var column_definition = MainGrid.ColumnDefinitions.Last();
            column_definition.Width = new GridLength(1, GridUnitType.Auto);
            column_definition.MaxWidth = 1;
            column_definition.MinWidth = 1;
            GridSpliter.IsEnabled = false;
        }
        else
        {
            MessagesContainer.Visibility = Visibility.Visible;
            var column_definition = MainGrid.ColumnDefinitions.Last();
            column_definition.Width = new GridLength(3, GridUnitType.Star);
            column_definition.MaxWidth = 550;
            column_definition.MinWidth = 320;
            GridSpliter.IsEnabled = true;
        }

        /*        MessagesContainer.Visibility =
                    MessagesContainer.Visibility == Visibility.Visible ?
                    Visibility.Collapsed :
                    Visibility.Visible;*/
    }
}
