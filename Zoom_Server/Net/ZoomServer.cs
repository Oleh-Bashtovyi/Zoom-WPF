using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
using Zoom_Server.Net.Codes;
using Zoom_Server.Net.Packets;
namespace Zoom_Server.Net;
#pragma warning disable CS8618 


internal class ZoomServer
{

    private int _port;
    private string _host;
    private ILogger log;
    public bool IsRunning { get; private set; } = false;

    private UdpClient udpServer;
    private CancellationTokenSource _cts { get; set; } = new();
    private List<Client> Clients { get; } = new();
    private List<Meeting> Meetings { get; } = new();



    public ZoomServer(string host, int port, ILogger logger)
    {
        _host = host;
        _port = port;
        log = logger;
        udpServer = new UdpClient(_port);
    }


    #region Run\Stop
    public void Run()
    {
        if (IsRunning)
        {
            throw new Exception("Server is already running!");
        }

        _cts = new();
        Task.Run(() => PingClients(_cts.Token));
        Task.Run(() => Process(_cts.Token));
        FileManager.ClearTempFolder();
        IsRunning = true;
    }
    public void Stop()
    {
        if (!IsRunning)
        {
            throw new Exception("Server is not running!");
        }

        _cts.Cancel();
        IsRunning= false;
    }
    #endregion






    protected async Task Process(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var receivedResult = await udpServer.ReceiveAsync(token);
                    //log.LogSuccess($"Server received some data! Data size: {receivedResult.Buffer.Length} bytes");
                    await HandleRequest(receivedResult, token);
                }
                catch (Exception)
                {
                }
            }
        }
        catch (TaskCanceledException)
        {
            log.LogWarning("Server process was forcely cancelled!");
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





    private async Task PingClients(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < Meetings.Count; i++)
                {
                    var meeting = Meetings[i];
                    var clients = meeting.Clients.ToArray();

                    for (int j = 0; j < clients.Length; j++)
                    {
                        var client = clients[j];

                        if (client.CheckLastPong())
                        {
                            await udpServer.SendAsync(OpCode.PING.AsArray(), client.IPAddress);
                        }
                        else
                        {
                            await RemoveUserFromMeeting(meeting, client, token);
                            log.LogError($"User: {client.Id} was removed due to inactivity!");
                        }
                    }
                }
                log.LogWarning($"Pong processed!");
                await Task.Delay(5000, token);
            }
        }
        catch { }
    }







    private async Task HandleRequest(UdpReceiveResult udpResult, CancellationToken token)
    {
        using var bufMemory = new MemoryStream(udpResult.Buffer);
        using var br = new BinaryReader(bufMemory);

        try
        {
            var opCode = (OpCode)br.ReadByte();

            //==================================================================================================
            //----PING-PONG
            //==================================================================================================
            if (opCode == OpCode.PING)
            {
                await udpServer.SendAsync(OpCode.PONG.AsArray(), udpResult.RemoteEndPoint, token);
            }
            else if (opCode == OpCode.PONG)
            {
                var clientEP = udpResult.RemoteEndPoint;

                foreach (var client in Clients)
                {
                    if (client.IPAddress.Equals(clientEP))
                    {
                        client.UpdateLastPong();
                        break;
                    }
                }
            }
            //==================================================================================================
            //----AUDIO
            //==================================================================================================
            else if (opCode == OpCode.PARTICIPANT_SENT_AUDIO)
            {
                var userId = br.ReadInt32();   
                var meetingId = br.ReadInt32();
                var length = br.ReadInt32();
                var data = br.ReadBytes(length);
                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

                if (meeting != null)
                {
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_SENT_AUDIO);
                        bw.Write(userId);
                        bw.Write(data.Length);
                        bw.Write(data);
                        //log.LogError(string.Join(",", data));
                        await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_TURNED_MICROPHONE_ON)
            {
                await Handle_UserChangedStateOfMicrophone(true, br, token);
            }
            else if (opCode == OpCode.PARTICIPANT_TURNED_MICROPHONE_OFF)
            {
                await Handle_UserChangedStateOfMicrophone(false, br, token);
            }
            //==================================================================================================
            //----CAMERA
            //==================================================================================================
            else if (opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CREATE)
            {
                var userId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var numberOfCusters = br.ReadInt32();
                var framePosition = br.ReadInt32();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                var meeting = Meetings.Where(x => x.Id == meetingId).FirstOrDefault();
                log.LogSuccess($"Received request for frame creation: user:{userId} meeting:{meetingId} clusters:{numberOfCusters}");

                if (meeting != null)
                {
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_CAMERA_FRAME_CREATE);
                        bw.Write(userId);
                        bw.Write(numberOfCusters);
                        bw.Write(framePosition);
                        bw.Write(dataLength);
                        bw.Write(data);
                        await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE)
            {
                var userId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var framePosition = br.ReadInt32();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                var meeting = Meetings.Where(x => x.Id == meetingId).FirstOrDefault();
                log.LogSuccess($"Received request for frame update: user:{userId} meeting:{meetingId} frame_position:{framePosition}");

                if (meeting != null)
                {
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE);
                        bw.Write(userId);
                        bw.Write(framePosition);
                        bw.Write(dataLength);
                        bw.Write(data);
                        await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_TURNED_CAMERA_ON)
            {
                await Handle_UserChangedStateOfCamera(true, br, token);
            }
            else if (opCode == OpCode.PARTICIPANT_TURNED_CAMERA_OFF)
            {
                await Handle_UserChangedStateOfCamera(false, br, token);
            }
            //==================================================================================================
            //----SCREEN
            //==================================================================================================
            else if(opCode == OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME)
            {
                var userId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var numberOfCusters = br.ReadInt32();
                var meeting = Meetings.Where(x => x.Id == meetingId).FirstOrDefault();

                if(meeting != null && 
                   meeting.ScreenDemonstartor != null && 
                   meeting.ScreenDemonstartor.Id == userId)
                {
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME);
                        bw.Write(userId);
                        bw.Write(numberOfCusters);
                        await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                        //log.LogSuccess($"Received request for SCREEN frame creation: user:{userId} meeting:{meetingId} clusters:{numberOfCusters}");
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME)
            {
                var userId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var framePosition = br.ReadInt32();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                var meeting = Meetings.Where(x => x.Id == meetingId).FirstOrDefault();
                //log.LogSuccess($"Received request screen frame update. user:{userId} meeting:{meetingId} frame_position:{framePosition}");

                if (meeting != null && 
                    meeting.ScreenDemonstartor != null &&
                    meeting.ScreenDemonstartor.Id == userId)
                {
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME);
                        bw.Write(userId);
                        bw.Write(framePosition);
                        bw.Write(dataLength);
                        bw.Write(data);
                        await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON)
            {
                var userId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);
                //log.LogWarning($"Received request for demonstration start. user: {userId}, meeting: {meetingId}");

                if (meeting != null)
                {
                    if(meeting.ScreenDemonstartor != null)
                    {
                        if (meeting.ScreenDemonstartor.Id != userId)
                        {
                            using (var ms = new MemoryStream())
                            using (var bw = new BinaryWriter(ms))
                            {
                                bw.Write((byte)OpCode.ERROR);
                                bw.Write((byte)ErrorCode.SCREEN_CAPTURE_DOES_NOT_ALLOWED);
                                bw.Write("Screen capture is already taken!");
                                await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
                            }
                        }
                        else log.LogWarning("User already demstrate screen!");

                        return;
                    }

                    var user = meeting.Clients.FirstOrDefault(x => x.Id == userId);

                    if (user != null)
                    {
                        using (var ms = new MemoryStream())
                        using (var bw = new BinaryWriter(ms))
                        {
                            meeting.ScreenDemonstartor = user;
                            bw.Write((byte)OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON);
                            bw.Write(userId);
                            await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                        }

                        using (var ms = new MemoryStream())
                        using (var bw = new BinaryWriter(ms))
                        {
                            meeting.ScreenDemonstartor = user;
                            bw.Write((byte)OpCode.SUCCESS);
                            bw.Write((byte)ScsCode.SCREEN_DEMONSTRATION_ALLOWED);
                            bw.Write("Screen can be taken!");
                            await udpServer.SendAsync(ms.ToArray(), user.IPAddress, token);
                            log.LogWarning($"Demonstration was allowed for user: {userId}, meeting: {meetingId}");
                        }
                    }
                    else log.LogError($"Meeting does not have such user!");
                }
            }
            else if (opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF)
            {
                var userId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);
                //log.LogWarning($"Received request to stop screen demonstration. Meeting: {meetingId}, User: {userId}");

                if (meeting != null && 
                    meeting.ScreenDemonstartor != null && 
                    meeting.ScreenDemonstartor.Id == userId)
                {
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        meeting.ScreenDemonstartor = null;
                        log.LogError($"Screen demonstration was stoped by user: {userId}, meeting: {meetingId}");
                        bw.Write((byte)OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF);
                        bw.Write(userId);
                        await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                    }
                }
            }
            //==================================================================================================
            //----MESSAGES
            //==================================================================================================
            else if(opCode == OpCode.PARTICIPANT_MESSAGE_SEND_EVERYONE)
            {
                var userId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var message = br.ReadString();
                var meeting = Meetings.FirstOrDefault( x => x.Id == meetingId);

                if(meeting != null)
                {
                    var sender = meeting.Clients.FirstOrDefault(x => x.Id == userId);

                    if (sender != null)
                    {
                        using (var ms = new MemoryStream())
                        using (var bw = new BinaryWriter(ms))
                        {
                            bw.Write((byte)OpCode.PARTICIPANT_MESSAGE_SEND_EVERYONE);
                            bw.Write(sender.Username);
                            bw.Write(message);
                            await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                        }
                    }
                }
            }
            else if(opCode == OpCode.PARTICIPANT_MESSAGE_SEND)
            {
                var senderUserId = br.ReadInt32();
                var receiverUserId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var message = br.ReadString();
                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

                if (meeting != null)
                {
                    var receiver = meeting.Clients.FirstOrDefault(x => x.Id == receiverUserId);
                    var sender = meeting.Clients.FirstOrDefault(x => x.Id == senderUserId);

                    if(sender != null && receiver != null)
                    {
                        using (var ms = new MemoryStream())
                        using (var bw = new BinaryWriter(ms))
                        {
                            bw.Write((byte)OpCode.PARTICIPANT_MESSAGE_SEND);
                            bw.Write(sender.Username);
                            bw.Write(receiver.Username);
                            bw.Write(message);
                            await udpServer.SendAsync(ms.ToArray(), receiver.IPAddress, token);
                            await udpServer.SendAsync(ms.ToArray(), sender.IPAddress, token);
                        }
                    }
                }
            }
            //==================================================================================================
            //----FILES
            //==================================================================================================
            else if(opCode == OpCode.PARTICIPANT_SEND_FILE_PART)
            {
                var meetingId = br.ReadInt32();
                //log.LogSuccess($"Meeting id: {meetingId}");
                var fileId = br.ReadString();
                //log.LogSuccess($"File id: {fileId}");
                var cursorPosition = br.ReadInt64();
                //log.LogSuccess($"Cursor position: {cursorPosition}");
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                FileManager.WriteDataToFile(data, cursorPosition, meetingId, fileId);
            }
            else if (opCode == OpCode.PARTICIPANT_SEND_FILE_LAST)
            {
                var senderId = br.ReadInt32();
                var receiverId = br.ReadInt32();
                var fileName = br.ReadString();

                var meetingId = br.ReadInt32();
                //log.LogSuccess($"Meeting id: {meetingId}");
                var fileId = br.ReadString();
                //log.LogSuccess($"File id: {fileId}");
                var cursorPosition = br.ReadInt64();
                //log.LogSuccess($"Cursor position: {cursorPosition}");
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                FileManager.WriteDataToFile(data, cursorPosition, meetingId, fileId);


                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

                if (meeting != null)
                {
                    var sender = meeting.Clients.FirstOrDefault(x => x.Id == senderId);
                    var receiver = meeting.Clients.FirstOrDefault(x => x.Id == receiverId);

                    if(receiver != null)
                    {
                        using (var ms = new MemoryStream(16))
                        using (var bw = new BinaryWriter(ms))
                        {
                            bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_UPLOADED);
                            bw.Write(sender?.Username ?? "Unknown user");
                            bw.Write(fileName);
                            bw.Write(FileManager.GetFileSize(meetingId, fileId));
                            bw.Write(fileId);

                            log.LogSuccess("FILE UPLOAD, SENDING TO RECEIVER");
                            await udpServer.SendAsync(ms.ToArray(), receiver.IPAddress, token);
                        }
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_SEND_FILE_LAST_EVERYONE)
            {
                log.LogSuccess("SEND FILE LAST EVERYONE");
                var senderId = br.ReadInt32();
                log.LogSuccess($"Sender id: {senderId}");
                var fileName = br.ReadString();
                log.LogSuccess($"File name: {fileName}");
                //=================================================
                var meetingId = br.ReadInt32();
                log.LogSuccess($"Meeting id: {meetingId}");
                var fileId = br.ReadString();
                log.LogSuccess($"File id: {fileId}");
                var cursorPosition = br.ReadInt64();
                log.LogSuccess($"Cursor position: {cursorPosition}");

                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                FileManager.WriteDataToFile(data, cursorPosition, meetingId, fileId);

                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

                if (meeting != null)
                {
                    var sender = meeting.Clients.FirstOrDefault(x => x.Id == senderId);

                    if (sender != null)
                    {
                        using (var ms = new MemoryStream(16))
                        using (var bw = new BinaryWriter(ms))
                        {
                            bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_UPLOADED_EVERYONE);
                            bw.Write(sender.Username);
                            bw.Write(fileName);
                            log.LogSuccess("Receiveing file size...");
                            bw.Write(FileManager.GetFileSize(meetingId, fileId));
                            bw.Write(fileId);
                            log.LogSuccess("FILE UPLOAD, SENDING TO PARTICIPANTS");
                            await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                        }
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_SEND_FILE_DELETE)
            {
                var meetingId = br.ReadInt32();
                var fileId = br.ReadString();
                log.LogError($"RECEIVED REQUEST TO DELETE FILE!\n--Meeting id: {meetingId}\n--File id: {fileId}\n");
                FileManager.DeleteFile(meetingId, fileId);
            }
            else if (opCode == OpCode.PARTICIPANT_SEND_FILE_DOWNLOAD)
            {
                var meetingId = br.ReadInt32();
                var fileId = br.ReadString();
                var cursor = br.ReadInt64();
                log.LogSuccess($"RECEIVED REQUEST TO DOWNLOAD FILE!\n--Meeting id: {meetingId}\n--File id: {fileId}\n--Cursor: {cursor}");

                (var data, var bytesRead) = FileManager.GetFileData(meetingId, fileId, cursor);

                using (var ms = new MemoryStream(16))
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_DOWNLOAD);
                    bw.Write(fileId);
                    bw.Write(bytesRead);
                    bw.Write(data);
                    await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
                }
            }
















            //==================================================================================================
            //----PARTICIPATING
            //==================================================================================================
            else if (opCode == OpCode.PARTICIPANT_LEFT_MEETING)
            {
                await Handle_UserLeftMeeting(udpResult, br, token);
            }
            else if (opCode == OpCode.PARTICIPANT_JOINED_MEETING)
            {
                await Handle_UserJoinedMeeting(udpResult, br, token);
            }
            else if (opCode == OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING)
            {
                await Handle_JoinUsingCode(udpResult, br, token);
            }
            //==================================================================================================
            //----GENERAL
            //==================================================================================================
            else if (opCode == OpCode.CREATE_MEETING)
            {
                await Handle_MeetingCreation(udpResult, br, token);
            }
            else if (opCode == OpCode.CREATE_USER)
            {
                await Handle_UserCreation(udpResult, br, token);
            }
            else if (opCode == OpCode.CHANGE_USER_NAME)
            {
                await Handle_UserRename(udpResult, br, token);
            }
        }
        catch (Exception ex) 
        {
            log.LogError(ex.Message);
        }
    }

    /*                    if (method == "CREATE")
                        {
                            var name = br.ReadString();

                            var meeting = new MeetingData();
                            Meetings.Add(meeting);

                            FileManager.CreateMeetingCatalog(meeting.Id);

                            meeting.AddClient(new MeetingUser(clientEP, name));

                            using (var response_ms = new MemoryStream(16))
                            using (var bw = new BinaryWriter(response_ms))
                            {
                                bw.Write("CONNECTED");
                                bw.Write(meeting.Id);
                                bw.Write(clientEP.Address.GetAddressBytes());
                                bw.Write(clientEP.Port);

                                await Instance.SendAsync(response_ms.ToArray(), clientEP);
                            }
                        }
                        else if (method == "CONNECT")
                        {
                            var meetingId = br.ReadInt64();
                            var name = br.ReadString();

                            var meeting = Meetings.Find(x => x.Id == meetingId);

                            if (meeting != null)
                            {
                                meeting.AddClient(new MeetingUser(clientEP, name));

                                using (var response_ms = new MemoryStream(8))
                                using (var bw = new BinaryWriter(response_ms))
                                {
                                    bw.Write("CONNECTED");
                                    bw.Write(meeting.Id);
                                    bw.Write(clientEP.Address.GetAddressBytes());
                                    bw.Write(clientEP.Port);

                                    await Instance.SendAsync(response_ms.ToArray(), clientEP);
                                }

                                using (var response_ms = new MemoryStream(64))
                                using (var bw = new BinaryWriter(response_ms))
                                {
                                    bw.Write("UPDATE_LIST_ONCONNECT");  // Header
                                    bw.Write(meeting.IsScreenShared);
                                    bw.Write(meeting.Clients.Count);    // Clients Count

                                    for (var i = 0; i < meeting.Clients.Count; i++)
                                    {
                                        bw.Write(meeting.Clients[i].Name);                                      // Client Name
                                        bw.Write(meeting.Clients[i].IpEndPoint.Address.GetAddressBytes());      // Client Address (4 bytes)
                                        bw.Write(meeting.Clients[i].IpEndPoint.Port);                           // Client Port
                                        bw.Write(meeting.Clients[i].IsUsingCamera);
                                        bw.Write(meeting.Clients[i].IsUsingAudio);
                                    }

                                    await Instance.SendAsync(response_ms.ToArray(), clientEP);
                                }

                                using (var broadcast_ms = new MemoryStream(64))
                                using (var bw = new BinaryWriter(broadcast_ms))
                                {
                                    bw.Write("UPDATE_LIST");            // Header
                                    bw.Write(meeting.Clients.Count);    // Clients Count

                                    for (var i = 0; i < meeting.Clients.Count; i++)
                                    {
                                        bw.Write(meeting.Clients[i].Name);                                      // Client Name
                                        bw.Write(meeting.Clients[i].IpEndPoint.Address.GetAddressBytes());      // Client Address (4 bytes)
                                        bw.Write(meeting.Clients[i].IpEndPoint.Port);                           // Client Port
                                    }

                                    await Instance.BroadcastAsync(broadcast_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                                }
                            }*/









    private SemaphoreSlim semaphore { get; } = new(1);
    private async Task Handle_UserCreation(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            var userName = br.ReadString();
            var client = new Client(udpResult.RemoteEndPoint, userName);
            Clients.Add(client);
            bw.Write(OpCode.CREATE_USER.AsByte());
            bw.Write(client.Id);
            bw.Write(userName);
            await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
        }
    }
    private async Task Handle_UserRename(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        var userPacket = UserPacket.ReadPacket(br);
        var user = Clients.FirstOrDefault(x => x.Id == userPacket.Id);

        if (user != null)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                user.Username = userPacket.Username;
                bw.Write((byte)OpCode.CHANGE_USER_NAME);
                userPacket.WriteToStream(bw);
                await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
            }
        }
    }
    private async Task Handle_MeetingCreation(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            var newMeeting = new Meeting();
            Meetings.Add(newMeeting);
            FileManager.CreateMeetingCatalog(newMeeting.Id);

            bw.Write((byte)OpCode.CREATE_MEETING);
            bw.Write(newMeeting.Id);
            //log.LogWarning($"Sending new meeting info: id:{newMeeting}");
            await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
        }
    }
    private async Task Handle_JoinUsingCode(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        var meetingId = br.ReadInt32();
        var meeting   = Meetings.FirstOrDefault(x => x.Id == meetingId);   

        if(meeting != null)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((byte)OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING);
                bw.Write(meetingId);
                //log.Log($"Somebody asked to enter meeting room! Room id:{meetingId}");
                await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
            }
        }
    }
    private async Task Handle_UserJoinedMeeting(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        var userId      = br.ReadInt32();
        var meetingCode = br.ReadInt32();
        var meeting     = Meetings.Where(x => x.Id == meetingCode).FirstOrDefault();
        //log.Log($"Somebody said that he has entered meeting room! User:{userId} Meeting:{meetingCode}");

        if (meeting != null)
        {
            var client = Clients.FirstOrDefault(x => x.Id == userId);

            if (client != null)
            {
                meeting.AddParticipant(client);
                await BroadCastParticipantJoin(meetingCode, token);
                //log.LogSuccess($"USer with id: {userId} joined meeting: {meetingCode}!");
            }
            //else log.LogError($"There is no such client! Clients: [{string.Join(", ", Clients.Select(x => x.Id))}]");
        }
        //else log.LogError($"No Such meeting!: Available meetings: [{string.Join(", ", Meetings.Select(x => x.Id))}]");
    }
    private async Task Handle_UserLeftMeeting(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        //log.LogWarning("Recived request for meeting leaving");
        var userId =    br.ReadInt32();
        var meetingId = br.ReadInt32();
        var meeting = Meetings.Where(x => x.Id == meetingId).FirstOrDefault();

        if (meeting != null)
        {
            var user = meeting.Clients.Where(x => x.Id == userId).FirstOrDefault();

            if(user != null)
            {
                await RemoveUserFromMeeting(meeting, user, token);
            }
        }
    }
    private async Task Handle_UserChangedStateOfCamera(bool newState, BinaryReader br, CancellationToken token)
    {
        var userId = br.ReadInt32();
        var meetingId = br.ReadInt32();
        var meeting = Meetings.Where(x => x.Id == meetingId).FirstOrDefault();

        if (meeting != null)
        {
            var user = meeting.Clients.Where(x => x.Id == userId).FirstOrDefault();

            if (user != null)
            {
                user.IsCameraOn = newState;

                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    var code = newState ? (byte)OpCode.PARTICIPANT_TURNED_CAMERA_ON : (byte)OpCode.PARTICIPANT_TURNED_CAMERA_OFF;
                    bw.Write(code);
                    bw.Write(userId);
                    await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                }
            }
        }
    }
    private async Task Handle_UserChangedStateOfMicrophone(bool newState, BinaryReader br, CancellationToken token)
    {
        var userId = br.ReadInt32();
        var meetingId = br.ReadInt32();
        var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

        if (meeting != null)
        {
            var user = meeting.Clients.FirstOrDefault(x => x.Id == userId);

            if (user != null)
            {
                user.IsMicrophoneOn = newState;

                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    var code = newState ? (byte)OpCode.PARTICIPANT_TURNED_MICROPHONE_ON : (byte)OpCode.PARTICIPANT_TURNED_MICROPHONE_OFF;
                    bw.Write(code);
                    bw.Write(userId);
                    //log.LogWarning($"User: {userId}, meeting: {meetingId} Turned microphon: {newState}");
                    await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                }
            }
        }
    }




    private async Task RemoveUserFromMeeting(Meeting meeting, Client user, CancellationToken token)
    {
        try
        {
            await semaphore.WaitAsync();

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                var participants = meeting.Clients.ToArray();
                meeting.RemoveParticipant(user);
                //log.LogWarning($"User: {userId} leaves meeting: {meetingId}");

                bw.Write((byte)OpCode.PARTICIPANT_LEFT_MEETING);
                bw.Write(user.Id);
                await BroadcastPacket(ms.ToArray(), participants, token);
            }
        }
        catch
        {
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }








    private async Task BroadcastPacket(byte[] packet, IEnumerable<Client> clients, CancellationToken token)
    {
        foreach (var participant in clients)
        {
            await udpServer.SendAsync(packet, participant.IPAddress, token);
        }
    }





    private async Task BroadCastParticipantJoin(int meetingId, CancellationToken token)
    {
        var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

        if(meeting != null)
        {
            using var pb = new PacketBuilder();

            foreach (var participant in meeting.Clients)
            {
                foreach (var participant_2 in meeting.Clients)
                {
                    pb.Clear();
                    pb.Write(OpCode.PARTICIPANT_JOINED_MEETING);
                    pb.Write_UserInfo(participant_2.Id, participant_2.Username);
                    //log.LogSuccess($"Broadcating joining info about user:{participant_2.Id} to user: {participant.Id}");
                    await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
                }
            }
        }
    }
}
#pragma warning restore CS8618
