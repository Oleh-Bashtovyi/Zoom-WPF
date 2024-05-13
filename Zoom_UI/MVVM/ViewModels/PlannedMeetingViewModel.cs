namespace Zoom_UI.MVVM.ViewModels;

public class PlannedMeetingViewModel : ViewModelBase
{
    private DateTime _plannedTime;
    private string? _description;

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

    public PlannedMeetingViewModel(DateTime plannedTime, string? description)
    {
        PlannedTime = plannedTime;
        Description = description;
    }
}
