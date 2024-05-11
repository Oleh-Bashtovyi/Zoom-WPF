using System.Net;
namespace Zoom_Server.Net;


internal class Client
{
    public int Id { get; set; }
    public string Username { get; set; }
    public bool IsCameraOn {  get; set; }
    public bool IsMicrophoneOn {  get; set; }
    public int MeetingId => Meeting?.Id ?? -1;
    public IPEndPoint IPAddress { get; set; }
    public Meeting? Meeting { get; set; } 


    public Client(IPEndPoint iPEndPoint, string username)
    {
        Id = IdGenerator.NewId();
        Username = username;
        IPAddress = iPEndPoint;
    }
}