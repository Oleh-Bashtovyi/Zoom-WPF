using System.Net.Sockets;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
using Zoom_Server.Net.Codes;
using Zoom_Server.Net.Packets;
namespace Zoom_Server.Net;
#pragma warning disable CS8618 


internal class UdpServer : OneProcessServer
{
    public class FileBuilder
    {
        public FrameBuilder FrameBuilder { get; set; }
        public string FileName { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
    }



    private UdpClient udpServer;


    //Collections
    private HashSet<int> MeetingsIds { get; } = new();
    private List<Client> Clients { get; } = new();
    private List<Meeting> Meetings { get; } = new();

    private Dictionary<int, UserScreenCapture> Meeting_ScreenCapture { get; } = new();

    private Dictionary<int, FileBuilder> User_FileBuilder { get; } = new();





    private class UserScreenCapture
    {
        public int UserId { get; set; }
        public FrameBuilder ScreenFrame { get; set; }


        public UserScreenCapture(int userId, FrameBuilder screenFrame)
        {
            UserId = userId;
            ScreenFrame = screenFrame;
        }
    }



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
                var length = br.ReadInt32();
                var data = br.ReadBytes(length);
                var user = Clients.FirstOrDefault(x => x.Id == userId);

                if(user != null && user.MeetingId > 0)
                {
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_SENT_AUDIO);
                        bw.Write(userId);
                        bw.Write(data.Length);
                        bw.Write(data);
                        await BroadcastPacket(ms.ToArray(), Clients.Where(x => x.MeetingId == user.MeetingId), token);
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
            else if (opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON)
            {
                var userId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var meeting = Meetings.Where(x => x.Id == meetingId).FirstOrDefault();
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
            else if (opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF)
            {
                var userId = br.ReadInt32();
                var meetingId = br.ReadInt32();
                var meeting = Meetings.Where(x => x.Id == userId).FirstOrDefault();

                if (meeting != null && 
                    meeting.ScreenDemonstartor != null && 
                    meeting.ScreenDemonstartor.Id == userId)
                {
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        meeting.ScreenDemonstartor = null;
                        bw.Write((byte)OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF);
                        bw.Write(userId);
                        await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
                    }
                }
            }
            //==================================================================================================
            //----MESSAGES
            //==================================================================================================




            else if (opCode == OpCode.PARTICIPANT_MESSAGE_SENT_EVERYONE)
            {
                var fromUserId = br.ReadInt32();
                var toUserId = br.ReadInt32();
                var message = br.ReadString();
                var fromClient = Clients.FirstOrDefault(x => x.Id ==  fromUserId);  
                var toClient = Clients.FirstOrDefault(x => x.Id ==  toUserId);  

                if(fromClient != null && fromClient.MeetingId > 0)
                {
                    if(toClient != null)
                    {
                        using var pb = new PacketBuilder();
                        pb.Write(OpCode.PARTICIPANT_MESSAGE_SENT_EVERYONE);  //Code
                        pb.Write(fromClient.Id);                             //From user (id)
                        pb.Write(toClient.Id);                               //to user (id)
                        pb.Write(message);                                   //Message
                        await udpServer.SendAsync(pb.ToArray(), fromClient.IPAddress, token);
                        await udpServer.SendAsync(pb.ToArray(), toClient.IPAddress, token);
                    }
                    else
                    {
                        using var pb = new PacketBuilder();
                        pb.Write(OpCode.PARTICIPANT_MESSAGE_SENT_EVERYONE);  //Code
                        pb.Write(fromClient.Id);                             //From user (id)
                        pb.Write(-1);                                   //to user (id)
                        pb.Write(message);                                   //Message

                        foreach (var participant in Clients.Where(x => x.MeetingId == fromClient.MeetingId))
                        {
                            await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
                        }
                    }
                }
            }
            else if(opCode == OpCode.PARTICIPANT_FILE_SEND_FRAME_CREATE)
            {
                var fromUser = br.ReadInt32();
                var toUser = br.ReadInt32();
                var numberOfClusters = br.ReadInt32();
                var fileName = br.ReadString();
                var client = Clients.FirstOrDefault(x => x.Id == fromUser);


                if (client != null && client.MeetingId > 0)
                {
                    var fileBuilder = new FileBuilder();
                    fileBuilder.FileName = fileName;
                    fileBuilder.ToUserId = toUser;
                    fileBuilder.FromUserId = fromUser;
                    fileBuilder.FrameBuilder = new(numberOfClusters);
                    User_FileBuilder[fromUser] = fileBuilder;
                }
            }
            else if(opCode == OpCode.PARTICIPANT_FILE_SEND_FRAME_UPDATE)
            {
/*                var frameData = br.ReadUserFrame();
                var frames = User_FileBuilder.GetValueOrDefault(frameData.UserId);

                if (frames != null)
                {
                    frames.FrameBuilder.AddFrame(frameData.Position, frameData.Data);

                    if (frames.FrameBuilder.IsFull)
                    {
                        log.LogSuccess("All frame received!");

                        await BroadcastFileToParticipants(frames, token);
                    }
                }*/
            }











            else if (opCode == OpCode.PARTICIPANT_LEFT_MEETING)
            {
                await Handle_UserLeftMeeting(udpResult, br, token);
            }
            else if (opCode == OpCode.PARTICIPANT_JOINED_MEETING)
            {
                await HANDLE_UserJoinedMeeting(udpResult, br, token);
            }
            else if (opCode == OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING)
            {
                await HANDLE_JoinUsingCode(udpResult, br, token);
            }
            else if (opCode == OpCode.CREATE_MEETING)
            {
                await HANDLE_MeetingCreation(udpResult, br, token);
            }
            else if (opCode == OpCode.CREATE_USER)
            {
                await HANDLE_UserCreation(udpResult, br, token);
            }
            else if (opCode == OpCode.CHANGE_USER_NAME)
            {
                await HANDLE_UserRename(udpResult, br, token);
            }
        }
        catch (Exception ex) 
        {
            log.LogError(ex.Message);
        }
    }



















    private async Task HANDLE_UserCreation(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
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
    private async Task HANDLE_UserRename(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
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
    private async Task HANDLE_MeetingCreation(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            var newMeeting = new Meeting();
            Meetings.Add(newMeeting);
            bw.Write((byte)OpCode.CREATE_MEETING);
            bw.Write(newMeeting.Id);
            log.LogWarning($"Sending new meeting info: id:{newMeeting}");
            await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
        }
    }


    private async Task HANDLE_JoinUsingCode(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        var meetingId = br.ReadInt32();

        if (MeetingsIds.Contains(meetingId))
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write((byte)OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING);
            bw.Write(meetingId);
            log.Log($"Somebody asked to enter meeting room! Room id:{meetingId}");
            await udpServer.SendAsync(ms.ToArray(), udpResult.RemoteEndPoint, token);
        }
    }
    private async Task HANDLE_UserJoinedMeeting(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        var userId = br.ReadInt32();
        var meetingCode = br.ReadInt32();

        log.Log($"Somebody said that he has entered meeting room! User:{userId} Meeting:{meetingCode}");

        var meeting = Meetings.Where(x => x.Id == meetingCode).FirstOrDefault();

        if (meeting != null)
        {
            var client = Clients.FirstOrDefault(x => x.Id == userId);

            if (client != null)
            {
                meeting.AddParticipant(client);

                log.LogSuccess($"USer with id: {userId} joined meeting: {meetingCode}!");

                await BroadCastParticipantJoin(meetingCode, token);
            }
            else
            {
                throw new Exception($"There is no such client! Clients: [{string.Join(", ", Clients.Select(x => x.Id))}]");
            }
        }
        else log.LogError($"No Sych meeting!: Available meetings: [{string.Join(", ", MeetingsIds)}]");
    }



    private async Task Handle_UserLeftMeeting(UdpReceiveResult udpResult, BinaryReader br, CancellationToken token)
    {
        var userId = br.ReadInt32();
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
                    meeting.RemoveParticipant(user);
                    bw.Write((byte)OpCode.PARTICIPANT_LEFT_MEETING);
                    bw.Write(user.Id);
                    await BroadcastPacket(ms.ToArray(), meeting.Clients, token);
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
        var meeting = Meetings.Where(x => x.Id == meetingId).FirstOrDefault();

        if (meeting != null)
        {
            var user = meeting.Clients.Where(x => x.Id == userId).FirstOrDefault();

            if (user != null)
            {
                user.IsMicrophoneOn = newState;

                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    var code = newState ? (byte)OpCode.PARTICIPANT_TURNED_MICROPHONE_ON : (byte)OpCode.PARTICIPANT_TURNED_MICROPHONE_OFF;
                    bw.Write(code);
                    bw.Write(userId);
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
        var participants = Clients.Where(x => x.MeetingId == meetingId);

        using var pb = new PacketBuilder();

        foreach(var participant in participants)
        {
            foreach(var participant_2 in participants)
            {
                pb.Clear();
                pb.Write(OpCode.PARTICIPANT_JOINED_MEETING);
                pb.Write_UserInfo(participant_2.Id, participant_2.Username);
                log.LogSuccess($"Broadcating joining info about user:{participant_2.Id} to user: {participant.Id}");
                await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
            }
        }
    }
    private async Task BroadcastFileToParticipants(FileBuilder fileBuilder, CancellationToken token)
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
    }
    private async Task BroadcastFrameToParticipants(
        OpCode FRMAE_CREATE_CODE,
        OpCode FRAME_UPDATE_CODE,
        int userId,
        IEnumerable<Client> participants, 
        FrameBuilder builder, 
        CancellationToken token)
    {
        var frames = builder.GetFrames();
        using var pb = new PacketBuilder();

        log.LogWarning("Process of sending frame begun!");
        foreach (var participant in participants)
        {
            pb.Clear();
            pb.Write(FRMAE_CREATE_CODE);
            pb.Write(userId);
            pb.Write(frames.Count());
            await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);

            for (int i = 0; i < frames.Length; i++)
            {
                pb.Clear();
                pb.Write(FRAME_UPDATE_CODE);
                pb.Write_UserFrame(userId, i, frames[i]);
                await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
            }

            log.LogSuccess($"Frame was sent to user: id:{participant.Id} name:{participant.Username}");
        }
    }
}
#pragma warning restore CS8618
