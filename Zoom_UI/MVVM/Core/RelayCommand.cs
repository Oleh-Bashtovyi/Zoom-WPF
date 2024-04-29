using System.Windows.Input;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.MVVM.Core;

internal class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    public void Execute(object? parameter)
    {
        _execute();
    }
}


internal class FileRelayCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;
    private readonly Action<FIleModel> _execute; 


    public FileRelayCommand(Action<FIleModel> execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute)); ;
    }

    public bool CanExecute(object? parameter)
    {
        return parameter is FIleModel;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter as FIleModel);
    }
}