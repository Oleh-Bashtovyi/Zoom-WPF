using System.Net;
namespace Zoom_Server.Net;


internal class Client
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public string Username { get; set; }
    public IPEndPoint IPAddress { get; set; }


    public Client(IPEndPoint iPEndPoint, string username)
    {
        Id = IdGenerator.NewId();
        MeetingId = -1;
        Username = username;
        IPAddress = iPEndPoint;
    }
}