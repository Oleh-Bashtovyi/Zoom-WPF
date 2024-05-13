using System.Net.Sockets;
using System.Reflection;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
using Zoom_Server.Net.Codes;
using Zoom_Server.Net.Packets;
namespace Zoom_Server.Net;
#pragma warning disable CS8618 


internal class UdpServer : OneProcessServer
{
    private UdpClient udpServer;
    private List<Client> Clients { get; } = new();
    private List<Meeting> Meetings { get; } = new();



    public UdpServer(string host, int port, ILogger logger) : base(host, port, logger)
    {
        udpServer = new UdpClient(_port);
    }

    protected override async Task Process(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var receivedResult = await udpServer.ReceiveAsync(token);
                    log.LogSuccess($"Server received some data! Data size: {receivedResult.Buffer.Length} bytes");
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



    private async Task HandleRequest(UdpReceiveResult udpResult, CancellationToken token)
    {
        using var bufMemory = new MemoryStream(udpResult.Buffer);
        using var br = new BinaryReader(bufMemory);

        try
        {
            var opCode = (OpCode)br.ReadByte();

            //==================================================================================================
            //----AUDIO
            //==================================================================================================
            if (opCode == OpCode.PARTICIPANT_SENT_AUDIO)
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
                        log.LogSuccess($"Received request for SCREEN frame creation: user:{userId} meeting:{meetingId} clusters:{numberOfCusters}");
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
                log.LogSuccess($"Received request screen frame update. user:{userId} meeting:{meetingId} frame_position:{framePosition}");

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
                log.LogWarning($"Received request for demonstration start. user: {userId}, meeting: {meetingId}");

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
                log.LogWarning($"Received request to stop screen demonstration. Meeting: {meetingId}, User: {userId}");

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



            else if(opCode == OpCode.PARTICIPANT_FILE_SEND_REQUEST_EVERYONE)
            {
                var senderUserId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var localId = br.ReadInt32();
                var numberOfClusters = br.ReadInt32();
                var fileName = br.ReadString();
                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

                if(meeting != null)
                {
                    var sender = meeting.Clients.FirstOrDefault(x => x.Id == senderUserId);

                    if(sender != null)
                    {
                        var frameBuilder = new FrameBuilder(numberOfClusters);
                        var fileBuilder = new FileBuilder(frameBuilder, fileName, sender);
                        meeting.FileBuilders.Add(fileBuilder);

                        using (var ms = new MemoryStream())
                        using (var bw = new BinaryWriter(ms))
                        {
                            bw.Write((byte)OpCode.PARTICIPANT_FILE_SEND_REQUEST);
                            bw.Write(localId);
                            bw.Write(fileBuilder.Id);
                            await udpServer.SendAsync(ms.ToArray(), sender.IPAddress, token);
                        }
                    }
                }
            }


            else if(opCode == OpCode.PARTICIPANT_FILE_SEND_REQUEST)
            {

            }
            else if(opCode == OpCode.PARTICIPANT_FILE_SEND_FRAME_UPDATE)
            {
                var meetingId = br.ReadInt32();
                var fileId = br.ReadInt32();
                var position = br.ReadInt32();
                var clusterSize = br.ReadInt32();
                var data = br.ReadBytes(clusterSize);
                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

                if(meeting != null)
                {
                    var fileBuilder = meeting.FileBuilders.FirstOrDefault(x => x.Id == fileId);

                    if(fileBuilder != null)
                    {
                        var frameBuilder = fileBuilder.FrameBuilder;
                        frameBuilder.AddFrame(position, data);

                        if (frameBuilder.IsFull)
                        {
                            using (var ms = new MemoryStream())
                            using (var bw = new BinaryWriter(ms))
                            {
                                bw.Write((byte)OpCode.PARTICIPANT_FILE_SEND);            //OpCode
                                bw.Write(fileBuilder.Sender.Username);                   //Sender: username
                                bw.Write(fileBuilder.Receiver?.Username ?? string.Empty);//Reciever: username
                                bw.Write(fileBuilder.Id);                                //fileId
                                bw.Write(frameBuilder.GetCountOfBytes());                //file_length
                                bw.Write(fileBuilder.FileName);                          //file_name

                                if (fileBuilder.IsToEveryone)
                                {
                                    await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                                }
                                else
                                {
                                    await udpServer.SendAsync(ms.ToArray(), fileBuilder.Sender.IPAddress, token);
                                    await udpServer.SendAsync(ms.ToArray(), fileBuilder.Receiver!.IPAddress, token);
                                }
                            }
                        }
                    }
                }
            }
            else if(opCode == OpCode.PARTICIPANT_FILE_DOWNLOAD_START)
            {
                var meetingId = br.ReadInt32();
                var fileId = br.ReadInt32();
                var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

                if(meeting != null)
                {
                    var fileBuilder = meeting.FileBuilders.FirstOrDefault(x => x.Id == fileId);

                    if(fileBuilder != null && fileBuilder.FrameBuilder.IsFull)
                    {
                        var frameBuilder = fileBuilder.FrameBuilder;
                        var frames = frameBuilder.GetFrames();
                        using (var ms = new MemoryStream())
                        using (var bw = new BinaryWriter(ms))
                        {
                            bw.Write((byte)OpCode.PARTICIPANT_FILE_DOWNLOAD_START);
                            bw.Write(fileId);
                            await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);

                            for (int i = 0; i < frames.Length; i++)
                            {
                                var frame = frames[i];
                                ms.Clear();
                                bw.Write((byte)OpCode.PARTICIPANT_FILE_DOWNLOAD_FRAME);
                                bw.Write(fileId);
                                bw.Write(i);
                                bw.Write(frame.Length);
                                bw.Write(frame);
                                await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
                            }
                        }
                    }
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








/*    private async Task Handle_QuestionCode(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        var question = (QstCode)br.ReadByte();
        
        if(question == QstCode.IS_CAMERAS_OF_USERS_STILL_ACTIVE)
        {
            var meetingId = br.ReadInt32();
            var meeting = Meetings.FirstOrDefault(x => x.Id == meetingId);

            if(meeting != null)
            {
                using (var ms = new  MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    var participants = meeting.Clients.ToArray();
                    bw.Write((byte)OpCode.QUESTION_CHECKOUT);
                    bw.Write((byte)QstCode.IS_CAMERAS_OF_USERS_STILL_ACTIVE);
                    bw.Write(participants.Length);

                    foreach (var participant in participants)
                    {
                        bw.Write(participant.Id);
                        bw.Write(participant.IsCameraOn);
                    }

                    await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
                }
            }
        }
    }*/












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
/*    private async Task BroadcastFileToParticipants(FileBuilder fileBuilder, CancellationToken token)
    {
        IEnumerable<Client> participants;


        if (fileBuilder.ToUserId > 0)
        {
            participants = Clients.Where(x => x.Id == fileBuilder.FromUserId || x.Id == fileBuilder.ToUserId);
        }
        else
        {
            var meetingId = Clients.FirstOrDefault(x => x.Id == fileBuilder.FromUserId)?.MeetingId ?? -1;

            if(meetingId <= 0)
            {
                participants = [];
            }
            else
            {
                participants = Clients.Where(x => x.MeetingId ==  meetingId);   
            }
        }

        using var pb = new PacketBuilder();
        var frameBuilder = fileBuilder.FrameBuilder;
        var frames = frameBuilder.GetFrames();

        foreach (var participant in participants)
        {
            pb.Clear(); 
            pb.Write(OpCode.PARTICIPANT_FILE_SEND_FRAME_CREATE);
            pb.Write(fileBuilder.FromUserId);
            pb.Write(fileBuilder.ToUserId);
            pb.Write(frameBuilder.NumberOfFrames);
            pb.Write(fileBuilder.FileName);
            await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);

            for (var i = 0; i < frameBuilder.NumberOfFrames; i++)
            {
                pb.Clear();
                pb.Write(OpCode.PARTICIPANT_FILE_SEND_FRAME_UPDATE);
                pb.Write_UserFrame(fileBuilder.FromUserId, i, frames[i]);
                await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
                log.LogSuccess($"Broadcasted frame: {i} to user: {participant.Id}");
            }
        }
    }*/
}
#pragma warning restore CS8618
