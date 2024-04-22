using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Zoom_UI.MVVM.ViewModels;

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void SetAndNotifyPropertyChanged<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
    {
        if(!value?.Equals(property) ?? false)
        {
            property = value;
            OnPropertyChanged(propertyName);
        }
    }
}
