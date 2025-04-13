using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI.MVVM.Models;

public class MeetingInfo
{
    public int Id { get; set; }
    public UserViewModel? ScreenDemonstrator { get; set; }
    public UserViewModel CurrentUser { get; set; }
    public List<UserViewModel> Participants { get; set; }


    public MeetingInfo(int id, UserViewModel currentUser)
    {
        Id = id;
        CurrentUser = currentUser;
        Participants = [];
    }

    public MeetingInfo(int id, UserViewModel currentUser, List<UserViewModel> participants, UserViewModel? screenDemonstrator)
    {
        Id = id;
        CurrentUser = currentUser;
        ScreenDemonstrator = screenDemonstrator;
        Participants = participants;
    }
}
