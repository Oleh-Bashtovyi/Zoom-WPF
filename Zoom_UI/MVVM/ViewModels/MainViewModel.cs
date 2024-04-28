using Zoom_UI.ClientServer;
using Zoom_UI.MVVM.Core;

namespace Zoom_UI.MVVM.ViewModels;

public class MainViewModel : ViewModelBase
{

    private ApplicationData _data;

    public ViewModelBase? CurrentViewModel => _data.Navigator.CurrentViewModel;



    public MainViewModel(ApplicationData data)
    {
        _data = data;
        _data.Navigator.OnCurrentViewModelChanged += () => OnPropertyChanged(nameof(CurrentViewModel));
    }
}
