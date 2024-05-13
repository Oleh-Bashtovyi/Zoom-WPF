using System.Net;
namespace Zoom_Server.Net;


internal class Client
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    public int Id { get; set; }
    public string Username { get; set; }
    public bool IsCameraOn {  get; set; }
    public bool IsMicrophoneOn {  get; set; }
    public int MeetingId => Meeting?.Id ?? -1;
    public IPEndPoint IPAddress { get; set; }
    public Meeting? Meeting { get; set; } 
    public DateTime LastPong { get; private set; }


    public Client(IPEndPoint iPEndPoint, string username)
    {
        Id = IdGenerator.NewId();
        Username = username;
        IPAddress = iPEndPoint;
        LastPong = DateTime.UtcNow;
    }



    public bool CheckLastPong()
    {
        return DateTime.UtcNow - LastPong < _timeout;
    }
    public void UpdateLastPong()
    {
        LastPong = DateTime.UtcNow;
    }
}