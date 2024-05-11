namespace Zoom_Server.Net;

internal class Meeting
{
    internal int Id { get; }
    private List<Client> _clients { get; }

    public Meeting()
    {
        Id = IdGenerator.NewId();
        _clients = new();
    }

    public IEnumerable<Client> Clients => _clients.AsEnumerable();


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
