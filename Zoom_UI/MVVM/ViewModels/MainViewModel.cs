using Zoom_UI.ClientServer;
using Zoom_UI.MVVM.Core;

namespace Zoom_UI.MVVM.ViewModels;

public class MainViewModel : ViewModelBase
{

    private UdpComunicator _comunicator;
    private ViewModelNavigator _navigator;

    public ViewModelBase? CurrentViewModel => _navigator.CurrentViewModel;



    public MainViewModel(UdpComunicator comunicator, ViewModelNavigator navigator)
    {
        _comunicator = comunicator;
        _navigator = navigator;
        _navigator.OnCurrentViewModelChanged += () => OnPropertyChanged(nameof(CurrentViewModel));
    }
}
