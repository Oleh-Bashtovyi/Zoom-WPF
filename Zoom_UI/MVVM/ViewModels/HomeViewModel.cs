using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Zoom_UI.ClientServer;
using Zoom_UI.MVVM.Core;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.MVVM.ViewModels;

#pragma warning disable CS8618 

public class HomeViewModel : ViewModelBase, ISeverEventSubsribable
{
    private string _meetingCodeToJoin;
    private string _usernameChangeField;
    private string _plannedMeetingDescription;
    private DateTime _plannedMeetingDate = DateTime.Now;
    private ZoomClient _comunicator;
    private ViewModelNavigator _navigator;
    private ApplicationData _applicationData;


    public bool UsernameIsNotEmpty => !string.IsNullOrWhiteSpace(Username);
    public string GetScheduleFile => $"./schedule.txt";
    public string Username
    {
        get => _usernameChangeField;
        set
        {
            SetAndNotifyPropertyChanged(ref _usernameChangeField, value);
            OnPropertyChanged(nameof(UsernameIsNotEmpty));
        }
    }
    public string PlannedMeetingDescription
    {
        get => _plannedMeetingDescription;
        set => SetAndNotifyPropertyChanged(ref _plannedMeetingDescription, value);
    }
    public string MeetingCodeToJoin
    {
        get => _meetingCodeToJoin;
        set => SetAndNotifyPropertyChanged(ref _meetingCodeToJoin, value);
    }
    public DateTime PlannedMeetingDate
    {
        get => _plannedMeetingDate;
        set => SetAndNotifyPropertyChanged(ref _plannedMeetingDate, value);
    }
    public ObservableCollection<PlannedMeetingViewModel> PlannedMeetings { get; } = new();
    public ICommand CreateNewMeetingCommand { get; }
    public ICommand JoinMeetingUsingCodeCommand { get; }
    public ICommand CreateNewPlannedMeetingCommand { get; }
    public ICommand RemovePlannedMeetingCommand {  get; }


    public HomeViewModel(ApplicationData data)
    {
        _applicationData = data;
        _comunicator = data.Comunicator;
        _navigator = data.Navigator;
        MeetingCodeToJoin = "1001";


        CreateNewMeetingCommand = new RelayCommand(
            () => _comunicator.Send_CreateMeeting(Username),
            () => UsernameIsNotEmpty);

        JoinMeetingUsingCodeCommand = new RelayCommand(
            () => 
            { 
                if(int.TryParse(MeetingCodeToJoin, out int code))
                {
                    _comunicator.Send_JoinMeetingUsingCode(code, Username); 
                }
                else
                {
                    MessageBox.Show("Can not parse string!");
                }
            },
            () => UsernameIsNotEmpty && !string.IsNullOrWhiteSpace(MeetingCodeToJoin));

        CreateNewPlannedMeetingCommand = new RelayCommand(
            CreateNewPlannedMeeting,
            () => !string.IsNullOrEmpty(PlannedMeetingDescription));

        RemovePlannedMeetingCommand = new PlannedMeetingRelayCommand(RemovePlannedMeeting);
        Username = "My user";

        if (File.Exists(GetScheduleFile))
        {
            ReadSchedules(GetScheduleFile);
        }
        else
        {
            File.Create(GetScheduleFile);
        }
    }


    private void ReadSchedules(string file)
    {
        var lines = File.ReadAllLines(file);

        PlannedMeetings.Clear();

        for (int i = 0; i < lines.Length; i += 2)
        {
            if (i + 1 < lines.Length)
            {
                var description = lines[i];
                var dateStrin = lines[i + 1];

                if (DateTime.TryParse(dateStrin, out DateTime date))
                {
                    PlannedMeetings.Add(new(date, description));
                }
            }
        }
    }

    private void CreateNewPlannedMeeting()
    {
        var plan = new PlannedMeetingViewModel(PlannedMeetingDate, PlannedMeetingDescription);
        PlannedMeetings.Add(plan);
        WriteSchedule(GetScheduleFile);
    }

    private void RemovePlannedMeeting(PlannedMeetingViewModel model)
    {
        var result = PlannedMeetings.Remove(model);

        if (result)
        {
            WriteSchedule(GetScheduleFile);
        }
    }

    private void WriteSchedule(string file)
    {
        using var stream = new StreamWriter(file);

        foreach (var plan in PlannedMeetings)
        {
            stream.WriteLine(plan.Description);
            stream.WriteLine(plan.PlannedTime.Date);
        }
    }


    void ISeverEventSubsribable.SubscribeEvents()
    {
        _comunicator.OnCurrentUser_JoinedToMeeting += _comunicator_OnCurrentUser_JoinedToMeeting;
    }

    void ISeverEventSubsribable.UnsubscribeEvents()
    {
        _comunicator.OnCurrentUser_JoinedToMeeting -= _comunicator_OnCurrentUser_JoinedToMeeting;
    }

    private void _comunicator_OnCurrentUser_JoinedToMeeting(MeetingInfo meeting)
    {
        _navigator.CurrentViewModel = new MeetingViewModel(_applicationData, meeting);
    }
}
#pragma warning restore CS8618 
