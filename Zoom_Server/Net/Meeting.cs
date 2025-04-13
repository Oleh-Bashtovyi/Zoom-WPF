namespace Zoom_Server.Net;

internal class Meeting
{
    private List<Client> _clients;
    internal int Id { get; }
    internal Client? ScreenDemonstartor { get; set; }
    internal bool IsScreenDemonstrationActive => ScreenDemonstartor != null;
    internal IEnumerable<Client> Clients => _clients.AsEnumerable();

    internal Meeting()
    {
        Id = IdGenerator.NewId();
        _clients = new();
    }

    internal void AddParticipant(Client participant)
    {
        _clients.Add(participant);
    }

    internal void RemoveParticipant(Client participant)
    {
        var result = _clients.Remove(participant);

        if(result && ScreenDemonstartor == participant)
        {
            ScreenDemonstartor = null;
        }
    }
}
