namespace Zoom_Server.Net;

internal class Meeting
{
    private List<Client> _clients;
    internal int Id { get; }
    public List<FileBuilder> FileBuilders { get; private set; }
    internal Client? ScreenDemonstartor { get; set; }
    public IEnumerable<Client> Clients => _clients.AsEnumerable();



    public Meeting()
    {
        Id = IdGenerator.NewId();
        _clients = new();
        FileBuilders = new();
    }




    public void AddParticipant(Client participant)
    {
        if(participant.Meeting != null)
        {
            if(participant.Meeting == this)
            {
                return;
            }
            participant.Meeting.RemoveParticipant(participant);
        }

        _clients.Add(participant);
        participant.Meeting = this;
    }

    public bool RemoveParticipant(Client participant)
    {
        var result = _clients.Remove(participant);

        if (result)
        {
            participant.Meeting = null;
        }

        return result;
    }
}
