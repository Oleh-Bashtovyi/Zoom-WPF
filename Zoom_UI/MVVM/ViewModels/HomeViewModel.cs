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
    private UserViewModel _currentUser;
    private UdpComunicator _comunicator;
    private ViewModelNavigator _navigator;
    private ApplicationData _applicationData;


    public bool IsConnected => CurrentUser?.Id > 0;
    public string UserId => (CurrentUser?.Id ?? 0) <= 0 ? string.Empty : CurrentUser!.Id.ToString();
    public string UsernameChangeField
    {
        get => _usernameChangeField;
        set => SetAndNotifyPropertyChanged(ref _usernameChangeField, value);
    }
    public string MeetingCodeToJoin
    {
        get => _meetingCodeToJoin;
        set => SetAndNotifyPropertyChanged(ref _meetingCodeToJoin, value);
    }
    public UserViewModel CurrentUser
    {
        get => _currentUser;
        set => SetAndNotifyPropertyChanged(ref _currentUser, value);
    }

    public ICommand ConnectToServerCommand { get; }
    public ICommand CreateNewMeetingCommand { get; }
    public ICommand JoinMeetingUsingCodeCommand { get; }
    public ICommand CreateNewPlannedMeetingCommand { get; }
    public ICommand JoinPlannedMeetingCommand { get; }
    public ICommand ChangeNameCommand { get; }



    public HomeViewModel(ApplicationData data)
    {
        _applicationData = data;
        _comunicator = data.Comunicator;
        _navigator = data.Navigator;
        CurrentUser = data.CurrentUser;
        CurrentUser.IsCurrentUser = true;
        MeetingCodeToJoin = "1002";


        ChangeNameCommand = new RelayCommand(
            () => Task.Run(async () => await _comunicator.SEND_CHANGE_NAME(CurrentUser.Id, UsernameChangeField)),
            () => IsConnected && !string.IsNullOrWhiteSpace(UsernameChangeField));

        ConnectToServerCommand = new RelayCommand(
            () => Task.Run(async () => await _comunicator.SEND_CREATE_USER(UsernameChangeField)),
            () => !IsConnected && !string.IsNullOrWhiteSpace(UsernameChangeField));

        CreateNewMeetingCommand = new RelayCommand(
            () => Task.Run(async () => await _comunicator.SEND_CREATE_MEETING()),
            () => IsConnected
            );
        JoinMeetingUsingCodeCommand = new RelayCommand(
            () => Task.Run(async () => 
            { 
                if(int.TryParse(MeetingCodeToJoin, out int code))
                {
                    await _comunicator.SEND_JOIN_MEETING_USING_CODE(code); 
                }
            }),
            () => IsConnected && !string.IsNullOrWhiteSpace(MeetingCodeToJoin)
            );
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

    void ISeverEventSubsribable.Unsubscribe()
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
