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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebEye.Controls.Wpf;
using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for MeetingView.xaml
    /// </summary>
    public partial class MeetingView : System.Windows.Controls.UserControl
    {
        public MeetingView()
        {
            InitializeComponent();
        }

        private void MessagesSectionButton_Click(object sender, RoutedEventArgs e)
        {
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
        }



        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SizeChanged += UserControl_SizeChanged;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SizeChanged -= UserControl_SizeChanged;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update the MainWindow size
            Window mainWindow = Window.GetWindow(this);

            if (mainWindow != null)
            {
                mainWindow.MinWidth = MinWidth + 30;
                mainWindow.MinHeight = MinHeight + 30;
            }
        }
    }
}
