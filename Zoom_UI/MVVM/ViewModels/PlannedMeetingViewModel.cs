namespace Zoom_UI.MVVM.ViewModels;

class PlannedMeetingViewModel : ViewModelBase
{
    private int _id;
    private DateTime _plannedTime;
    private string? _description;


    public int Id
    {
        get => _id;
        set => SetAndNotifyPropertyChanged(ref  _id, value);    
    }

    public DateTime PlannedTime
    {
        get => _plannedTime;
        set => SetAndNotifyPropertyChanged(ref _plannedTime, value);
    }

    public string? Description
    {
        get => _description;
        set => SetAndNotifyPropertyChanged(ref _description, value);
    }
}
