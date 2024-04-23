using ChatServer.Net.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Zoom_Server.Logging;

namespace Zoom_Server.Net;











internal class Server
{
    private int _port;
    private string _host;
    private ILogger log;
    private UdpClient udpServer;
    private Task? _runningProcess;


    /*    ConcurrentBag<T>: A thread-safe unordered collection of elements.
    ConcurrentDictionary<TKey, TValue>: A thread-safe dictionary.*/

    /*    private SemaphoreSlim _clients_semaphore = new(1);
        private SemaphoreSlim _meetings_semaphore = new(1);*/
    // private List<Client> Clients { get; } = new();
    /*    private Dictionary<string, HashSet<Client>> Meetings { get; } = new();*/

    private ConcurrentBag<Client> Clients { get; } = new();
    private CancellationTokenSource _cancellationTokenSource { get; set; } = new();
    public bool IsRunning => _runningProcess != null && !_runningProcess.IsCompleted;


    public Server(string host, int port, ILogger logger)
    {
        log = logger;
        _host = host;
        _port = port;
        udpServer = new UdpClient(_port);
    }


    #region Run\Stop
    internal void Run()
    {
        if (IsRunning)
        {
            throw new Exception("Server is already running!");
        }

        _cancellationTokenSource = new();
        _runningProcess = Task.Run(() => TcpListeningProcess(_cancellationTokenSource.Token));
    }
    internal void Stop()
    {
        if (!IsRunning)
        {
            throw new Exception("Server is not running!");
        }

        _cancellationTokenSource?.Cancel();
        _runningProcess = null;
    }
    #endregion






    private async Task TcpListeningProcess(CancellationToken token)
    {
        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(new IPEndPoint(IPAddress.Parse(_host), _port));
            listener.Start(5);
            log.LogSuccess("SERVER STARTED!");

            while (true)
            {
                log.LogSuccess($"Waiting for connection via port {_port}");
                var clientSocket = await listener.AcceptTcpClientAsync(token);
                var remoteEndPoint = clientSocket.Client.RemoteEndPoint as IPEndPoint;
                log.Log($"Accepted tcp client with IP: {remoteEndPoint?.Address}, Port: {remoteEndPoint?.Port}");
                var client = new Client(this);
                client.Tcp.Connect(clientSocket);
                _ = Task.Run(() => HandleClientUsingTcpAsync(client, token));
            }
        }
        catch (OperationCanceledException)
        {
            log.LogWarning("Server process was forcely cancelled!");
        }
        catch (Exception ex)
        {
            log.LogError(ex.ToString());
        }
        finally
        {
            log.LogWarning("Initiating stop process...");
            await Task.Delay(3000);
            listener?.Dispose();
            log.LogWarning("Server was stopped!");
        }
    }





    private async Task HandleClientUsingTcpAsync(Client client, CancellationToken token)
    {
        try
        {
            var socket = client.Tcp.Socket;
            var stream = socket.GetStream();

            while(!token.IsCancellationRequested)
            {
                var data = await ReadFromStream(stream, token);

                log.LogSuccess("Very big file received! Size: " + data.Length);

                if(data.Length == 0)
                {
                    await Task.Delay(1000000);
                }
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex.Message);
            client.Tcp.Socket.Close();
        }
    }


    static async Task<byte[]> ReadFromStream(NetworkStream stream,CancellationToken token)
    {
        byte[] buffer = new byte[4096];

        using (MemoryStream memoryStream = new MemoryStream())
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);

                if (stream.DataAvailable)
                {
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, bytesRead);
                    buffer = newBuffer;
                }
            }
            return memoryStream.ToArray();
        }
    }



        private async Task UdpListenProcess(CancellationToken token)
    {
        try
        {



            /*            while (true)
                        {
                            await Console.Out.WriteLineAsync("Server listening...");
                            var receivedResult = await udpServer.ReceiveAsync(token);
                            await Console.Out.WriteLineAsync("Server received some info...");

                            using var ms = new MemoryStream(receivedResult.Buffer);
                            using var br = new BinaryReader(ms);

                            var code = br.ReadByte();
                            var strLength = br.ReadInt32(); // Read the length prefix
                            var strBytes = br.ReadBytes(strLength); // Read the string bytes
                            var str = Encoding.UTF8.GetString(strBytes); // Decode the string
                            await Console.Out.WriteLineAsync("Info have read!");
                            log.LogSuccess($"Code received: {code}, String received: {str}");

                            //udpServer.Send(receivedData, receivedData.Length, clientEndPoint);
                        }*/

            while (true)
            {
                var clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

                await Console.Out.WriteLineAsync("Server listening...");
                var receivedResult = await udpServer.ReceiveAsync(token);
                await Console.Out.WriteLineAsync("Server received some info...");

                using var ms = new MemoryStream(receivedResult.Buffer);
                using var br = new BinaryReader(ms);

                var code = br.ReadByte();
                var str = br.ReadString();
                await Console.Out.WriteLineAsync("Info have read!");
                log.LogSuccess($"Code received: {code}, String received: {str}");
            }





            /*            var ipHost = Dns.GetHostEntry(_host);
                        var ipAddr = ipHost.AddressList[0];
                        listener = new TcpListener(ipAddr, _port);
                        listener.Start(5);
                        log.LogSuccess("SERVER STARTED!");

                        while (true)
                        {
                            log.LogSuccess($"Waiting for connection via port {_port}");
                            var clientSocket = await listener.AcceptTcpClientAsync(token);
                            var remoteEndPoint = clientSocket.Client.RemoteEndPoint as IPEndPoint;
                            log.Log($"Accepted tcp client with IP: {remoteEndPoint?.Address}, Port: {remoteEndPoint?.Port}");


                            var client = new Client(clientSocket, this);
                            _ = Task.Run(() => client.Process(token));
                        }*/
        }
        catch (OperationCanceledException)
        {
            log.LogWarning("Server process was forcely cancelled!");
        }
        catch (Exception ex)
        {
            log.LogError(ex.ToString());
            await Console.Out.WriteLineAsync("Exception on server");
        }
        finally
        {
            log.LogWarning("Server was stopped!");
            await Console.Out.WriteLineAsync("Server stopped");
        }
    }



/*
    public async Task BroadCastJoinToMeeting(Client joiner, string meetingCode, CancellationToken token)
    {
        *//*        try
                {
                    await _meetings_semaphore.WaitAsync();


                    var meeting = Meetings.GetValueOrDefault(meetingCode);

                    if (meeting == null)
                    {
                        meeting = new HashSet<Client>(new ClientEqualityComparer());

                        meeting.Add(joiner);

                        var broadcastPacket = new PacketBuilder();
                        broadcastPacket.WriteOpCode(OpCode.ParticipantJoininMeeting);
                        broadcastPacket.WriteMessage(joiner.Username);
                        broadcastPacket.WriteMessage(joiner.UID.ToString());
                        joiner.CLientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                    }
                    else if (meeting.Add(joiner))
                    {
                        foreach (var participant in meeting)
                        {
                            foreach (var otherParticipant in meeting)
                            {
                                var broadcastPacket = new PacketBuilder();
                                broadcastPacket.WriteOpCode(OpCode.ParticipantJoininMeeting);
                                broadcastPacket.WriteMessage(otherParticipant.Username);
                                broadcastPacket.WriteMessage(otherParticipant.UID.ToString());
                                participant.CLientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    _meetings_semaphore.Release();
                }*//*
    }*/





/*    public async Task BroadCastFrame(Client client, string meetngCode, byte[] frame, CancellationToken token)
    {
        try
        {
            await _meetings_semaphore.WaitAsync();

            var meeting = Meetings.GetValueOrDefault(meetngCode);

            if (meeting != null)
            {
                foreach (var participant in meeting)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(OpCode.PatricipantCameraFrameSent);
                    broadcastPacket.WriteArray(frame);
                    participant.CLientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _meetings_semaphore.Release();
        }
    }*/
}

































/*internal class Server
{
    private int _port;
    private string _host;
    private ILogger log;
    private TcpListener _listener;
    private Task? _runningProcess;


*//*    ConcurrentBag<T>: A thread-safe unordered collection of elements.
ConcurrentDictionary<TKey, TValue>: A thread-safe dictionary.*/

/*    private SemaphoreSlim _clients_semaphore = new(1);
    private SemaphoreSlim _meetings_semaphore = new(1);*//*
   // private List<Client> Clients { get; } = new();
*//*    private Dictionary<string, HashSet<Client>> Meetings { get; } = new();*//*

    private ConcurrentBag<Client> Clients { get; } = new();
    private CancellationTokenSource _cancellationTokenSource { get; set; } = new();
    public bool IsRunning => _runningProcess != null && !_runningProcess.IsCompleted;


    public Server(string host, int port, ILogger logger)
    {
        log = logger;
        _host = host;
        _port = port;
        _listener = new TcpListener(IPAddress.Parse(_host), _port);
    }


    #region Run\Stop
    internal void Run()
    {
        if (IsRunning)
        {
            throw new Exception("Server is already running!");
        }

        _cancellationTokenSource = new();
        _runningProcess = Task.Run(() => Process(_cancellationTokenSource.Token));
    }
    internal void Stop()
    {
        if (!IsRunning)
        {
            throw new Exception("Server is not running!");
        }

        _cancellationTokenSource?.Cancel();
        _runningProcess = null;
    }
    #endregion




    private async Task Process(CancellationToken token)
    {
        TcpListener? listener = null;
        try
        {
            var ipHost = Dns.GetHostEntry(_host);
            var ipAddr = ipHost.AddressList[0];
            listener = new TcpListener(ipAddr, _port);
            listener.Start(5);
            log.LogSuccess("SERVER STARTED!");

            while (true)
            {
                log.LogSuccess($"Waiting for connection via port {_port}");
                var clientSocket = await listener.AcceptTcpClientAsync(token);
                var remoteEndPoint = clientSocket.Client.RemoteEndPoint as IPEndPoint;
                log.Log($"Accepted tcp client with IP: {remoteEndPoint?.Address}, Port: {remoteEndPoint?.Port}");


                var client = new Client(clientSocket, this);
                _ = Task.Run(() => client.Process(token));
            }
        }
        catch (OperationCanceledException)
        {
            log.LogWarning("Server process was forcely cancelled!");
        }
        catch (Exception ex)
        {
            log.LogError(ex.ToString());
        }
        finally
        {
            log.LogWarning("Initiating stop process...");
            await Task.Delay(3000);
            listener?.Dispose();
            log.LogWarning("Server was stopped!");
        }
    }




    public async Task BroadCastJoinToMeeting(Client joiner, string meetingCode, CancellationToken token)
    {
*//*        try
        {
            await _meetings_semaphore.WaitAsync();


            var meeting = Meetings.GetValueOrDefault(meetingCode);

            if (meeting == null)
            {
                meeting = new HashSet<Client>(new ClientEqualityComparer());

                meeting.Add(joiner);

                var broadcastPacket = new PacketBuilder();
                broadcastPacket.WriteOpCode(OpCode.ParticipantJoininMeeting);
                broadcastPacket.WriteMessage(joiner.Username);
                broadcastPacket.WriteMessage(joiner.UID.ToString());
                joiner.CLientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            }
            else if (meeting.Add(joiner))
            {
                foreach (var participant in meeting)
                {
                    foreach (var otherParticipant in meeting)
                    {
                        var broadcastPacket = new PacketBuilder();
                        broadcastPacket.WriteOpCode(OpCode.ParticipantJoininMeeting);
                        broadcastPacket.WriteMessage(otherParticipant.Username);
                        broadcastPacket.WriteMessage(otherParticipant.UID.ToString());
                        participant.CLientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _meetings_semaphore.Release();
        }*//*
    }





    public async Task BroadCastFrame(Client client, string meetngCode, byte[] frame, CancellationToken token)
    {
        try
        {
            await _meetings_semaphore.WaitAsync();

            var meeting = Meetings.GetValueOrDefault(meetngCode);

            if(meeting != null)
            {
                foreach (var participant in meeting)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(OpCode.PatricipantCameraFrameSent);
                    broadcastPacket.WriteArray(frame);
                    participant.CLientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _meetings_semaphore.Release();
        }
    }


    *//*    private static TcpListener _listener;

        private static List<Client> _users;
        static void Main(string[] args)
        {
            _users = new();
            _listener = new(IPAddress.Parse("127.0.0.1"), 7891);
            _listener.Start();

            while (true)
            {
                var client = new Client(_listener.AcceptTcpClient());
                _users.Add(client);

                *//*Broadcast the connection to everyone on the server*//*
                BroadcastConnection();
            }
        }


        private static void BroadcastConnection()
        {
            foreach (var user in _users)
            {
                foreach (var usr in _users)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(1);
                    broadcastPacket.WriteMessage(usr.Username);
                    broadcastPacket.WriteMessage(usr.UID.ToString());
                    user.CLientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                }
            }
        }


        public static void BroadcastMessage(string message)
        {
            foreach (var user in _users)
            {
                var msgPacket = new PacketBuilder();
                msgPacket.WriteOpCode(5);
                msgPacket.WriteMessage(message);
                user.CLientSocket.Client.Send(msgPacket.GetPacketBytes());
            }
        }

        public static void BroadcastDisconnect(string uid)
        {
            var disconnectedUser = _users.Where(x => x.UID.ToString() == uid).FirstOrDefault();

            if (disconnectedUser == null)
            {
                return;
            }

            _users.Remove(disconnectedUser);

            foreach (var user in _users)
            {
                var broadcastPacket = new PacketBuilder();
                broadcastPacket.WriteOpCode(10);
                broadcastPacket.WriteMessage(uid);
                user.CLientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            }

            BroadcastMessage($"[{disconnectedUser.Username}] Disconnected!");
        }*//*
}
*/