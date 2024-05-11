namespace Zoom_Server.Net;

internal class Meeting
{
    private List<Client> _clients;
    internal int Id { get; }
    internal Client? ScreenDemonstartor { get; set; }
    public IEnumerable<Client> Clients => _clients.AsEnumerable();



    public Meeting()
    {
        Id = IdGenerator.NewId();
        _clients = new();
    }



    public void AddParticipant(Client participant)
    {
        if(participant.Meeting != null)
        {
            participant.Meeting.RemoveParticipant(participant);
        }

        _clients.Add(participant);
        participant.Meeting = this;
    }

    public void RemoveParticipant(Client participant)
    {
        _clients.Remove(participant);
        participant.Meeting = null;
    }
}
