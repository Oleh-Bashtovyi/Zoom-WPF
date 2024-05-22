using System.Windows;
using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI;


public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if(DataContext is IDisposable mvm)
        {
            mvm.Dispose();
        }
    }
}