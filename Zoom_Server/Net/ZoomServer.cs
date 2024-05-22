using System.Net;
using System.Net.Sockets;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
using Zoom_Server.Net.Codes;
namespace Zoom_Server.Net;
#pragma warning disable CS8618 


internal class ZoomServer
{
    private readonly TimeSpan _clientsInactivityTimeout = TimeSpan.FromSeconds(25);

    private int _port;
    private string _host;
    private ILogger log;
    private UdpClient udpServer;
    private CancellationTokenSource _cts  = new();
    public bool IsRunning { get; private set; } = false;
    private List<Meeting> Meetings { get; } = new();
    private SemaphoreSlim Semaphor { get; } = new(1);
    public ZoomServer(string host, int port, ILogger logger)
    {
        _host = host;
        _port = port;
        log = logger;
        udpServer = new UdpClient(_port);
    }


    #region Run\Stop
    public void Start()
    {
        if (!IsRunning)
        {
            _cts = new();
            FileManager.ClearTempFolder();
            Task.Run(() => PingProcess(_cts.Token));
            Task.Run(() => ReceivingProcess(_cts.Token));
            IsRunning = true;
        }
    }
    public void Stop()
    {
        if (IsRunning)
        {
            _cts.Cancel();
            _cts.Dispose();
            IsRunning = false;
        }
    }
    #endregion






    private async Task PingProcess(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, token);
                
                await Semaphor.WaitAsync(token).ConfigureAwait(false); ;

                foreach(var meeting in Meetings)
                {
                    var clients = meeting.Clients.ToArray();

                    foreach(var client in clients)
                    {
                        if (client.CheckLastPong(_clientsInactivityTimeout))
                        {
                            await udpServer.SendAsync(OpCode.PING.AsArray(), client.IPAddress, token);
                        }
                        else
                        {
                            await Handle_RemoveUserFromMeeting(meeting, client, token);
                            log.LogError($"User: {client.Id} was removed due to inactivity!");
                        }
                    }
                }

                log.LogWarning($"Pong processed!");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError("(PING PROCESS) - "+ ex.Message);
            }
            finally
            {
                Semaphor.Release();
            }
        }
    }
    private async Task ReceivingProcess(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var receivedResult = await udpServer.ReceiveAsync(token);
                //_ = Task.Run(() => HandleRequest(receivedResult, token));
                await HandleRequest(receivedResult, token);
            }
            catch (OperationCanceledException)
            {
                log.LogWarning("(RECEIVING PROCESS) - " + "Server process was forcely cancelled!");
                break;
            }
            catch (Exception ex)
            {
                log.LogError("(RECEIVING PROCESS) - " + ex.Message);
            }
        }

        log.LogWarning("(RECEIVING PROCESS) - " + "Server receiving process stopped!");
    }



















    





    private async Task HandleRequest(UdpReceiveResult udpResult, CancellationToken token)
    {
        try
        {
            //await Semaphor.WaitAsync(token).ConfigureAwait(false);
            await Semaphor.WaitAsync(token);
            using var buffMem = new MemoryStream(udpResult.Buffer);
            using var br = new BinaryReader(buffMem);
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
                var handled = false;

                foreach(var meeting in Meetings)
                {
                    foreach (var client in meeting.Clients)
                    {
                        if (client.IPAddress.Equals(clientEP))
                        {
                            client.UpdateLastPong();
                            log.LogSuccess($"Received pong from user: {client.Id}, username: {client.Username}");
                            handled = true;
                            break;
                        }
                    }
                    if (handled) break;
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
                        await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
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
                        await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
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
                        await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
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
                var framePosition = br.ReadInt32();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
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
                        bw.Write(framePosition);
                        bw.Write(dataLength);
                        bw.Write(data);
                        await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
                        //log.LogError($"Received request to create frame. Clusters: {numberOfCusters}, User: {userId}, meeting: {meetingId}");
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
                        await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
                        //log.LogError($"Cluster {framePosition} was broadcasted!");
                    }
                }
                else log.LogError("ERROR: meeting does not exist or no demonstratio or wrong demonstrator!");
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
                            await Broadcast_Error(ErrorCode.SCREEN_CAPTURE_DOES_NOT_ALLOWED,
                                "Screen capture is already taken!", udpResult.RemoteEndPoint, token);
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
                            await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
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
                        await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
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
                            await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
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
                var fileId = br.ReadString();
                var cursorPosition = br.ReadInt64();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                FileManager.WriteDataToFile(data, cursorPosition, meetingId, fileId);
            }
            else if (opCode == OpCode.PARTICIPANT_SEND_FILE_LAST)
            {
                var senderId = br.ReadInt32();
                var receiverId = br.ReadInt32();
                var fileName = br.ReadString();
                //==============================
                var meetingId = br.ReadInt32();
                var fileId = br.ReadString();
                var cursorPosition = br.ReadInt64();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);
                FileManager.WriteDataToFile(data, cursorPosition, meetingId, fileId);

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
                            await udpServer.SendAsync(ms.ToArray(), receiver.IPAddress, token);
                        }
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_SEND_FILE_LAST_EVERYONE)
            {
                var senderId = br.ReadInt32();
                var fileName = br.ReadString();
                //=================================================
                var meetingId = br.ReadInt32();
                var fileId = br.ReadString();
                var cursorPosition = br.ReadInt64();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);
                FileManager.WriteDataToFile(data, cursorPosition, meetingId, fileId);

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
                            bw.Write(FileManager.GetFileSize(meetingId, fileId));
                            bw.Write(fileId);
                            await Broadcast_Packet(ms.ToArray(), meeting.Clients.Where(x => x.Id != senderId), token);
                        }
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_SEND_FILE_DELETE)
            {
                var meetingId = br.ReadInt32();
                var fileId = br.ReadString();
                FileManager.DeleteFile(meetingId, fileId);
            }
            else if (opCode == OpCode.PARTICIPANT_SEND_FILE_DOWNLOAD)
            {
                var meetingId = br.ReadInt32();
                var fileId = br.ReadString();
                var cursor = br.ReadInt64();
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
            else if (opCode == OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING)
            {
                await Handle_JoinUsingCode(udpResult, br, token);
            }
            else if (opCode == OpCode.CREATE_MEETING)
            {
                await Handle_MeetingCreation(udpResult, br, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) 
        {
            log.LogError("(CLIENT REQUEST HANDLING) - " + ex.Message);
        }
        finally
        {
            Semaphor.Release();
        }
    }

















    private async Task Handle_UserLeftMeeting(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        var userId =    br.ReadInt32();
        var meetingId = br.ReadInt32();
        var meeting = Meetings.Where(x => x.Id == meetingId).FirstOrDefault();

        if (meeting != null)
        {
            var user = meeting.Clients.Where(x => x.Id == userId).FirstOrDefault();

            if(user != null)
            {
                await Handle_RemoveUserFromMeeting(meeting, user, token);
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
                    await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
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
                    await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
                }
            }
        }
    }
    private async Task Handle_RemoveUserFromMeeting(Meeting meeting, Client user, CancellationToken token)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            var participants = meeting.Clients.ToArray();
            meeting.RemoveParticipant(user);
            bw.Write((byte)OpCode.PARTICIPANT_LEFT_MEETING);
            bw.Write(user.Id);
            await Broadcast_Packet(ms.ToArray(), participants, token);
        }
    }










    private async Task Handle_JoinUsingCode(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        var username = br.ReadString();
        var meetingId = br.ReadInt32();
        var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);
        log.LogSuccess($"Received request to join using code: {meetingId}, username: {username}");

        if (meeting != null)
        {
            var newUser = new Client(udpResult.RemoteEndPoint, username);
            meeting.AddParticipant(newUser);
            await Broadcast_UserJoinMeeting(meeting, newUser, token);
        }
        else
        {
            await Broadcast_Error(ErrorCode.MEETING_DOES_NOT_EXISTS,
    $"Such meeting ({meetingId}) does not exist!", udpResult.RemoteEndPoint, token);
            log.LogError($"No such meeting! exisiting meetings: {string.Join(',', Meetings.Select(x => x.Id))}");
        }
    }

    private async Task Handle_MeetingCreation(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        log.LogSuccess("received reqyest to create new meeting!");
        var userName = br.ReadString();
        var newMeeting = new Meeting();
        var newUser = new Client(udpResult.RemoteEndPoint, userName);
        Meetings.Add(newMeeting);
        newMeeting.AddParticipant(newUser);
        FileManager.CreateMeetingCatalog(newMeeting.Id);
        await Broadcast_MeetingInfoToConnectedUser(newMeeting, newUser, token);
    }





    private async Task Broadcast_Error(ErrorCode errorCode, string message, IPEndPoint remoteEndPoint, CancellationToken token)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.ERROR);
            bw.Write((byte)errorCode);
            bw.Write(message);
            await udpServer.SendAsync(ms.ToArray(), remoteEndPoint, token);
        }
    }
    




    private async Task Broadcast_Packet(byte[] packet, IEnumerable<Client> clients, CancellationToken token)
    {
        foreach (var participant in clients)
        {
            await udpServer.SendAsync(packet, participant.IPAddress, token);
        }
    }


    private async Task Broadcast_UserJoinMeeting(Meeting meeting, Client user, CancellationToken token)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter((ms)))
        {
            bw.Write((byte)OpCode.PARTICIPANT_JOINED_MEETING);
            bw.Write(user.Id);
            bw.Write(user.Username);

            await Broadcast_Packet(ms.ToArray(), meeting.Clients, token);
        }
        log.LogSuccess("Broadcasting info to user!");
        await Broadcast_MeetingInfoToConnectedUser(meeting, user, token);
    }


    private async Task Broadcast_MeetingInfoToConnectedUser(Meeting meeting, Client user, CancellationToken token)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter((ms)))
        {
            bw.Write((byte)OpCode.USER_CONNECTED_TO_MEETING);
            bw.Write(meeting.Id);

            if(meeting.IsScreenDemonstrationActive)
            {
                bw.Write(true);
                bw.Write(meeting.ScreenDemonstartor!.Id);
            }
            else
            {
                bw.Write(false);
            }

            bw.Write(user.Id);
            bw.Write(meeting.Clients.Count());

            foreach(var client in meeting.Clients)
            {
                bw.Write(client.Id);
                bw.Write(client.Username);
                bw.Write(client.IsMicrophoneOn);
                bw.Write(client.IsCameraOn);
            }
            log.LogSuccess("Sending info!!");
            await udpServer.SendAsync(ms.ToArray(), user.IPAddress, token);
        }
    }
}
#pragma warning restore CS8618
