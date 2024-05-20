using System.Net;
namespace Zoom_Server.Net;

internal class Client
{
    internal int Id { get; set; }
    internal string Username { get; set; }
    internal bool IsCameraOn {  get; set; }
    internal bool IsMicrophoneOn {  get; set; }
    internal IPEndPoint IPAddress { get; set; }
    internal DateTime LastPong { get; private set; }


    internal Client(IPEndPoint iPEndPoint, string username)
    {
        Id = IdGenerator.NewId();
        Username = username;
        IPAddress = iPEndPoint;
        LastPong = DateTime.UtcNow;
    }

    internal bool CheckLastPong(TimeSpan _timeout)
    {
        return DateTime.UtcNow - LastPong < _timeout;
    }
    internal void UpdateLastPong()
    {
        LastPong = DateTime.UtcNow;
    }
}