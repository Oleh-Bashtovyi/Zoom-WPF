using ChatClient.MVVM.Core;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Zoom_UI.MVVM.Core;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.MVVM.ViewModels;

public class MeetingViewModel : ViewModelBase
{
    private string _message;
    private User _selectedParticipant;

    public ObservableCollection<UserViewModel> Participants { get; private set; }
    public ObservableCollection<User> ParticipantsSelection { get; private set; }


    public string MeetingId {  get; private set; }
    public string Message
    {
        get => _message;
        set => SetAndNotifyPropertyChanged(ref _message, value);
    }

    public User SelectedParticipant
    {
        get => _selectedParticipant;
        set => SetAndNotifyPropertyChanged(ref _selectedParticipant, value);  
    }


    public UserViewModel CurrentUser {  get; private set; }

    public ICommand SendMessage { get; }




    public ICommand SwitchMicrophonState { get; }
    public ICommand SwitchCameraState { get; }
    public ICommand LeaveMeeting {  get; }



    public MeetingViewModel()
    {
        Participants = new();
        ParticipantsSelection = [new("All", "")];
        CurrentUser = new UserViewModel();
        CurrentUser.IsMicrophoneOn = false;
        CurrentUser.CameraImage = new(new("Assets/cam_on.png", UriKind.Relative));


        SwitchMicrophonState = new RelayCommandWithoutParameters(() => 
        {
            CurrentUser.IsMicrophoneOn = !CurrentUser.IsMicrophoneOn;
        });
    }


    private void AddUser(UserViewModel user)
    {
        Participants.Add(user);
        ParticipantsSelection.Add(user.User);
    }

    private void RemoveUser(UserViewModel user)
    {
        Participants.Remove(user);
        ParticipantsSelection.Remove(user.User);
    }
}
