using ChatClient.MVVM.Core;
using System.Windows.Input;
using Zoom_UI.MVVM.Core;

namespace Zoom_UI.MVVM.ViewModels;

#pragma warning disable CS8618 

public class StartWindowViewModel : ViewModelBase
{
    private string _username;
    private string _meetingCode;

    public string Username
    {
        get => _username;
        set => SetAndNotifyPropertyChanged(ref _username, value);
    }

    public string MeetingCode
    {
        get => _meetingCode;
        set => SetAndNotifyPropertyChanged(ref _meetingCode, value);
    }

    public ICommand CreateNewMeetingCommand { get; }
    public ICommand CreateNewPlannedMeetingCommand { get; }
    public ICommand JoinMeetingCommand { get; }



    public StartWindowViewModel()
    {
        /*        CreateNewMeetingCommand = new RelayCommand(o => { }, o => !string.IsNullOrWhiteSpace(Username));
                JoinMeetingCommand = new RelayCommand(o => { }, o => !string.IsNullOrWhiteSpace(Username));*/

        CreateNewMeetingCommand = new RelayCommand(CreateNewMeeting, CanCreateNewMeeting);
        JoinMeetingCommand = new RelayCommand(JoinMeeting, CanJoinMeeting);
    }

    private void CreateNewMeeting()
    {

    }

    private void JoinMeeting()
    {

    }

    private bool CanCreateNewMeeting()
    {
        return !string.IsNullOrWhiteSpace(Username);
    }

    private bool CanJoinMeeting()
    {
        return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(MeetingCode);
    }
}



#pragma warning restore CS8618 
