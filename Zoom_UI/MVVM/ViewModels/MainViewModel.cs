using Zoom_UI.MVVM.Core;

namespace Zoom_UI.MVVM.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{

    private ApplicationData _data;

    public ViewModelBase? CurrentViewModel => _data.Navigator.CurrentViewModel;



    public MainViewModel(ApplicationData data)
    {
        _data = data;
        _data.Navigator.OnCurrentViewModelChanged += () => OnPropertyChanged(nameof(CurrentViewModel));
    }

    public void Dispose()
    {
        if(CurrentViewModel is ISeverEventSubsribable subsribable)
        {
            subsribable.Unsubscribe();
        }

        if(CurrentViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        try
        {
            _data.Comunicator.Stop();
        }
        catch (Exception)
        {
        }
    }
}
