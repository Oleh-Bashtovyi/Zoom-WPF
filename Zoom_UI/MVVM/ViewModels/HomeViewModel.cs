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
    private UserViewModel _currentUser;
    private UdpComunicator _comunicator;
    private ViewModelNavigator _navigator;


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



    public HomeViewModel(UdpComunicator comunicator, ViewModelNavigator navigator)
    {
        _comunicator = comunicator;
        _navigator = navigator;

        CurrentUser = new();

        ChangeNameCommand = new RelayCommand(
            () => Task.Run(async () => await _comunicator.Send_ChangeName(CurrentUser.Id, UsernameChangeField)),
            () => IsConnected && !string.IsNullOrWhiteSpace(UsernameChangeField));

        ConnectToServerCommand = new RelayCommand(
            () => Task.Run(async () => await _comunicator.Send_CrateUser(UsernameChangeField)),
            () => !IsConnected && !string.IsNullOrWhiteSpace(UsernameChangeField));

        //CreateNewMeetingCommand = new RelayCommand(CreateNewMeeting, CanCreateNewMeeting);
        //JoinMeetingUsingCodeCommand = new RelayCommand(JoinMeeting, CanJoinMeeting);
    }


    void ISeverEventSubsribable.Subscribe()
    {
        _comunicator.OnUserCreated += OnUserConnected;
        _comunicator.OnUserChangedName += OnUserNameChanged;
    }


    void ISeverEventSubsribable.Unsubscribe()
    {
        _comunicator.OnUserCreated -= OnUserConnected;
        _comunicator.OnUserChangedName -= OnUserNameChanged;
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
