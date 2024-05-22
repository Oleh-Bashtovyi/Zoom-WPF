using System.Windows;
using Zoom_UI.MVVM.Core;

namespace Zoom_UI.MVVM.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private ApplicationData _data;
    public ViewModelBase? CurrentViewModel => _data.PagesNavigator.CurrentViewModel;

    public event Action? OnRecordingStarted;
    public event Action? OnRecordingFinished;


    public MainViewModel(ApplicationData data)
    {
        _data = data;
        _data.PagesNavigator.OnCurrentViewModelChanged += () =>
        {
            OnPropertyChanged(nameof(CurrentViewModel));

            if(CurrentViewModel is MeetingViewModel meetingModel)
            {
                meetingModel.OnRecordStarted -= OnRecordStarted;
                meetingModel.OnRecordFinished -= OnRecordFinished;
                meetingModel.OnRecordStarted += OnRecordStarted;
                meetingModel.OnRecordFinished += OnRecordFinished;
            }
        };
    }

    private void OnRecordStarted()
    {
        OnRecordingStarted?.Invoke();
    }

    private void OnRecordFinished()
    {
        OnRecordingFinished?.Invoke();
    }

    public void Dispose()
    {
        OnRecordingStarted = null;
        OnRecordingFinished = null;

        if(CurrentViewModel is ISeverEventSubsribable subsribable)
        {
            subsribable.UnsubscribeEvents();
        }

        if(CurrentViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        try
        {
            _data.ZoomClient.Stop();
        }
        catch { }
    }
}
