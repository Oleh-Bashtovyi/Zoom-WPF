using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WebEye.Controls.Wpf;
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
    private UserViewModel _currentUser;
    private ZoomClient _comunicator;
    private ViewModelNavigator _navigator;
    private ApplicationData _applicationData;


    public bool IsConnected => CurrentUser?.Id > 0;
    public string UserId => (CurrentUser?.Id ?? 0) <= 0 ? string.Empty : CurrentUser!.Id.ToString();
    public string UsernameChangeField
    {
        get => _usernameChangeField;
        set => SetAndNotifyPropertyChanged(ref _usernameChangeField, value);
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
    public UserViewModel CurrentUser
    {
        get => _currentUser;
        set => SetAndNotifyPropertyChanged(ref _currentUser, value);
    }
    
    public ObservableCollection<PlannedMeetingViewModel> PlannedMeetings { get; } = new();

    public string GetScheduleFile => $"./schedule_{CurrentUser.Id}.txt";


    public ICommand ConnectToServerCommand { get; }
    public ICommand CreateNewMeetingCommand { get; }
    public ICommand JoinMeetingUsingCodeCommand { get; }
    public ICommand CreateNewPlannedMeetingCommand { get; }
    public ICommand JoinPlannedMeetingCommand { get; }
    public ICommand ChangeNameCommand { get; }
    public ICommand RemovePlannedMeetingCommand {  get; }


    public HomeViewModel(ApplicationData data)
    {
        _applicationData = data;
        _comunicator = data.Comunicator;
        _navigator = data.Navigator;
        CurrentUser = data.CurrentUser;
        CurrentUser.IsCurrentUser = true;
        MeetingCodeToJoin = "1002";
        

        ChangeNameCommand = new RelayCommand(
            () => _comunicator.Send_ChangeName(CurrentUser.Id, UsernameChangeField),
            () => IsConnected && !string.IsNullOrWhiteSpace(UsernameChangeField));

        ConnectToServerCommand = new RelayCommand(
            () => _comunicator.Send_CreateUser(UsernameChangeField),
            () => !IsConnected && !string.IsNullOrWhiteSpace(UsernameChangeField));

        CreateNewMeetingCommand = new RelayCommand(
            () => _comunicator.Send_CreateMeeting(),
            () => IsConnected
            );
        JoinMeetingUsingCodeCommand = new RelayCommand(
            () => 
            { 
                if(int.TryParse(MeetingCodeToJoin, out int code))
                {
                    _comunicator.SendJoinMeetingUsingCode(code); 
                }
            },
            () => IsConnected && !string.IsNullOrWhiteSpace(MeetingCodeToJoin)
            );
        CreateNewPlannedMeetingCommand = new RelayCommand(
            CreateNewPlannedMeeting,
            () => IsConnected && !string.IsNullOrEmpty(PlannedMeetingDescription)
            );
        RemovePlannedMeetingCommand = new PlannedMeetingRelayCommand(RemovePlannedMeeting);

        if (IsConnected)
        {
            if (File.Exists(GetScheduleFile))
            {
                ReadSchedules(GetScheduleFile);
            }
            else
            {
                File.Create(GetScheduleFile);
            }
        }
        OnPropertyChanged(nameof(IsConnected));
    }


    private void RemovePlannedMeeting(PlannedMeetingViewModel model)
    {
        var result = PlannedMeetings.Remove(model);

        if (result)
        {
            WriteSchedule(GetScheduleFile);
        }
    }


    private void ReadSchedules(string file)
    {
        var lines = File.ReadAllLines(file);

        PlannedMeetings.Clear();

        for (int i = 0; i < lines.Length; i += 2)
        {
            if(i + 1 < lines.Length)
            {
                var description = lines[i];
                var dateStrin = lines[i + 1];

                if(DateTime.TryParse(dateStrin, out DateTime date))
                {
                    PlannedMeetings.Add(new(date, description));
                }
            }
        }
    }
    private void WriteSchedule(string file)
    {
        using var stream = new StreamWriter(file);

        foreach(var plan in PlannedMeetings)
        {
            stream.WriteLine(plan.Description);
            stream.WriteLine(plan.PlannedTime.Date);
        }
    }






    private void CreateNewPlannedMeeting()
    {
        var plan = new PlannedMeetingViewModel(PlannedMeetingDate, PlannedMeetingDescription);
        PlannedMeetings.Add(plan);
        WriteSchedule(GetScheduleFile);
    }


    void ISeverEventSubsribable.Subscribe()
    {
        _comunicator.OnUserCreated += OnUserConnected;
        _comunicator.OnUserChangedName += OnUserNameChanged;
        _comunicator.OnMeetingCreated += OnMeetingCreated;
        _comunicator.OnUserJoinedMeeting_UsingCode += OnMeetingCreated;
    }

    private void OnMeetingCreated(MeetingInfo meeting)
    {
        _navigator.CurrentViewModel = new MeetingViewModel(_applicationData, meeting);
    }

    void ISeverEventSubsribable.UnsubscribeEvents()
    {
        _comunicator.OnUserCreated -= OnUserConnected;
        _comunicator.OnUserChangedName -= OnUserNameChanged;
        _comunicator.OnMeetingCreated -= OnMeetingCreated;
        _comunicator.OnUserJoinedMeeting_UsingCode -= OnMeetingCreated;
    }


    private void OnUserConnected(UserModel user)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentUser.Username = user.Username;
            CurrentUser.Id = user.Id;
            OnPropertyChanged(nameof(UserId));
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(MeetingCodeToJoin));
            WriteSchedule(GetScheduleFile);
        });
    }

    private void OnUserNameChanged(UserModel user)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentUser.Username = user.Username;
            MessageBox.Show("Name changed!");
        });
    }





}



#pragma warning restore CS8618 
