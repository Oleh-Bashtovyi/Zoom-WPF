using Zoom_UI.MVVM.Core;

namespace Zoom_UI.MVVM.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private ApplicationData _data;
    public ViewModelBase? CurrentViewModel => _data.PagesNavigator.CurrentViewModel;
    
    public MainViewModel(ApplicationData data)
    {
        _data = data;
        _data.PagesNavigator.OnCurrentViewModelChanged += () => OnPropertyChanged(nameof(CurrentViewModel));
    }

    public void Dispose()
    {
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
        catch (Exception)
        {
        }
    }
}
