namespace Zoom_Server.Net;

internal class Meeting
{
    private List<Client> _clients;

    internal int Id { get; }
    internal Client? ScreenDemonstartor { get; set; }
    internal bool IsScreenDemonstrationActive => ScreenDemonstartor != null;
    internal IEnumerable<Client> Clients => _clients.AsEnumerable();
    internal DateTime LastActivity { get; private set; }

    internal Meeting()
    {
        Id = IdGenerator.NewId();
        _clients = new();
    }

    internal void AddParticipant(Client participant)
    {
        _clients.Add(participant);
        LastActivity = DateTime.Now;
    }

    internal void RemoveParticipant(Client participant)
    {
        _clients.Remove(participant);
        LastActivity = DateTime.Now;
    }

    internal bool CheckLastActivity(TimeSpan timeout)
    {
        return DateTime.Now - LastActivity < timeout;
    }
}
