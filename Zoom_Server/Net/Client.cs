using ChatServer.Net.IO;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace Zoom_Server.Net;


internal class ClientEqualityComparer : IEqualityComparer<Client>
{
    public bool Equals(Client? x, Client? y)
    {
        return x?.UID == y?.UID;
    }

    public int GetHashCode([DisallowNull] Client obj)
    {
        return obj.UID.GetHashCode();
    }
}






internal class Client
{
    public Guid UID { get; set; }
    public string MeetingId { get; set; }
    public string Username { get; set; }
    public Server Server { get; set; }
    public UDP Udp { get; private set; }
    public TCP Tcp {  get; private set; }


    public Client(Server server)
    {
        UID = Guid.NewGuid();
        Username = string.Empty;
        MeetingId = string.Empty;
        Server = server;
        Udp = new UDP();
        Tcp = new TCP();
    }

    public void SetUsername(string username)
    {
        Username = username;
    }


    public class TCP
    {
        public TcpClient Socket { get; set; }

        private NetworkStream _stream;

        public TCP() { }

        public void Connect(TcpClient socket)
        {
            Socket = socket;
            _stream = Socket.GetStream();
            
        }
    }





    public class UDP
    {
        public IPEndPoint? IPEndPoint { get; private set; }

        public UDP() { }


        public void Connect(IPEndPoint ep)
        {
            IPEndPoint = ep;
        }
    }




/*    public async Task Process(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {

            }
            catch (EndOfStreamException)
            {
                break;
            }


        }
    }*/
}









/*internal class Client
{
    private PacketReader _packetReader;
    public TcpClient CLientSocket { get; set; }
    public Server Server { get; set; }
    public Guid UID { get; set; }
    public string Username { get; set; } = string.Empty;    



    public Client(TcpClient clientSocket, Server server)
    {
        UID = Guid.NewGuid();
        Server = server;
        CLientSocket = clientSocket;
        _packetReader = new(clientSocket.GetStream());
    }


    public async Task Process(CancellationToken token)
    {
        while(!token.IsCancellationRequested)
        {
            try
            {

            }
            catch (EndOfStreamException)
            {
                break;
            }
            var opCode = _packetReader.ReadByte();

            
        }

       *//* while (true)
        {
            try
            {
                var opcode = _packetReader.ReadByte();

                switch (opcode)
                {
                    case 5:
                        var msg = _packetReader.ReadMessage();
                        Console.WriteLine($"[{DateTime.Now}]: Message received: {msg}");
                        Program.BroadcastMessage($"[{DateTime.Now}]: [{Username}]: {msg}");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"[{UID}]: Disconnected!");
                Program.BroadcastDisconnect(UID.ToString());
                CLientSocket.Close();
                break;
            }
        }*//*
    }
}
*/