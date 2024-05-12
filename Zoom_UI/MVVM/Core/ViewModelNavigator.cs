using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI.MVVM.Core;

public class ViewModelNavigator
{
    public ViewModelBase? _currentViewModel;
  
    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            if(_currentViewModel != value)
            {
                if(_currentViewModel is ISeverEventSubsribable subsribable)
                {
                    subsribable.UnsubscribeEvents();
                }

                _currentViewModel = value;

                if(_currentViewModel is ISeverEventSubsribable newSubsribable)
                {
                    newSubsribable.Subscribe();
                }

                NotifyCurrentViewModelChanged();
            }
        }
    }

    public event Action? OnCurrentViewModelChanged;

    private void NotifyCurrentViewModelChanged()
    {
        OnCurrentViewModelChanged?.Invoke();
    }
}
