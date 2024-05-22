using System.Windows;
using Zoom_UI.Managers;
using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI;


public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += MainWindow_Closed;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if(DataContext is MainViewModel viewModel)
        {
            viewModel.OnRecordingStarted += OnRecordStarted;
            viewModel.OnRecordingFinished += OnRecordFinished;
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if(DataContext is MainViewModel mvm)
        {
            mvm.OnRecordingStarted -= OnRecordStarted;
            mvm.OnRecordingFinished -= OnRecordFinished;
            mvm.Dispose();
        }
        
    }


    private void OnRecordStarted()
    {
        //MessageBox.Show("MAXIMIZATION!");
        Application.Current.MainWindow.WindowState = WindowState.Maximized;
        Application.Current.MainWindow.ResizeMode = ResizeMode.CanMinimize;
    }

    private void OnRecordFinished()
    {
        //MessageBox.Show("RETURN!");
        Application.Current.MainWindow.WindowState = WindowState.Normal;
        Application.Current.MainWindow.ResizeMode = ResizeMode.CanResize;
    }
}