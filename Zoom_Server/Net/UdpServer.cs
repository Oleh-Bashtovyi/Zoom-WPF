using System.Net;
using System.Net.Sockets;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;
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
    private Dictionary<int, UserScreenCapture> Meeting_ScreenCapture { get; } = new();
    private Dictionary<int, FrameBuilder> User_CameraFrame { get; } = new();
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



    private async Task HandleRequest(UdpReceiveResult asyncResult, CancellationToken token)
    {
        using var pr = new PacketReader(new MemoryStream(asyncResult.Buffer));

        try
        {
            var opCode = pr.ReadOpCode();

            if(opCode == OpCode.CREATE_USER)
            {
                //============================================================
                //CREATE USER:
                //input:
                //--opcode
                //--username;
                //output:
                //--opcode
                //--uid
                //--username
                //============================================================
                var userName = pr.ReadString();
                var client = new Client(asyncResult.RemoteEndPoint, userName);
                Clients.Add(client);
                using var pb = new PacketBuilder();
                pb.Write(OpCode.CREATE_USER.AsByte());
                pb.Write_UserInfo(client.Id, userName);
                log.LogWarning($"Sending new user info: id:{client.Id} username:{client.Username}");
                await udpServer.SendAsync(pb.ToArray(), asyncResult.RemoteEndPoint, token);
            }

            else if(opCode == OpCode.PARTICIPANT_SENT_AUDIO)
            {
                var userId = pr.ReadInt32();    
                var length = pr.ReadInt32();
                var data = pr.ReadBytes(length);

                var user = Clients.FirstOrDefault(x => x.Id == userId);

                if(user != null && user.MeetingId > 0)
                {
                    using var pb = new PacketBuilder();
                    pb.Write(OpCode.PARTICIPANT_SENT_AUDIO);
                    pb.Write(userId);
                    pb.Write(data.Length);
                    pb.Write(data);

                    foreach (var participant in Clients.Where(x => x.MeetingId == user.MeetingId))
                    {
                        await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
                    }
                }
            }

            else if(opCode == OpCode.CHANGE_USER_NAME)
            {
                var userInfo = pr.ReadUserInfo();
                var user = Clients.FirstOrDefault(x => x.Id == userInfo.Id);
                if(user != null)
                {
                    user.Username = userInfo.Username;
                    using var pb = new PacketBuilder();
                    pb.Write(OpCode.CHANGE_USER_NAME);
                    pb.Write_UserInfo(userInfo.Id, userInfo.Username);
                    await udpServer.SendAsync(pb.ToArray(), asyncResult.RemoteEndPoint, token);
                }
            }
            else if(opCode == OpCode.CREATE_MEETING)
            {
                //============================================================
                //CREATE MEETING:
                //input:
                //--op_code;
                //output:
                //--op_code;
                //--meeting_id;
                //============================================================
                var newMeeting = IdGenerator.NewId();
                MeetingsIds.Add(newMeeting);
                using var pb = new PacketBuilder();
                pb.Write(OpCode.CREATE_MEETING.AsByte());   
                pb.Write(newMeeting);                      
                log.LogWarning($"Sending new meeting info: id:{newMeeting}");
                await udpServer.SendAsync(pb.ToArray(), asyncResult.RemoteEndPoint, token);
            }
            else if(opCode == OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING)
            {
                //============================================================
                //JOIN MEETING USING CODE:
                //input:
                //--op_code;
                //--meeting_code;
                //output:
                //--SUCCESS:
                //-----op_code_join;
                //--FAIL:
                //-----nothing;
                //============================================================
                var meetingCode = pr.ReadInt32();

                if(MeetingsIds.Contains(meetingCode))
                {
                    using var pw = new PacketBuilder();
                    pw.Write(OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING);
                    pw.Write(meetingCode);
                    log.Log($"Somebody asked to enter meeting room! Room id:{meetingCode}");
                    await udpServer.SendAsync(pw.ToArray(), asyncResult.RemoteEndPoint, token);
                }
            }
            else if(opCode == OpCode.PARTICIPANT_JOINED_MEETING)
            {
                var userId = pr.ReadInt32();
                var meetingCode = pr.ReadInt32();

                log.Log($"Somebody said that he has entered meeting room! User:{userId} Meeting:{meetingCode}");

                if (MeetingsIds.Contains(meetingCode))
                {
                    var client = Clients.FirstOrDefault(x => x.Id == userId);

                    if (client != null)
                    {
                        client.MeetingId = meetingCode;

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
            else if(opCode == OpCode.PARTICIPANT_LEFT_MEETING)
            {
                var userInfo = pr.ReadUserInfo();
                var user = Clients.FirstOrDefault(x => x.Id == userInfo.Id);

                if(user != null && user.MeetingId > 0)
                {
                    using var pb = new PacketBuilder();
                    pb.Write(OpCode.PARTICIPANT_LEFT_MEETING);
                    pb.Write_UserInfo(userInfo.Id, userInfo.Username);

                    foreach (var participant in Clients.Where(x => x.MeetingId == user.MeetingId))
                    {
                        await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
                    }

                    user.MeetingId = -1;
                }

            }

            else if(opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CREATE)
            {
                //============================================================
                //CREATE CAMERA FRAME:
                //input:
                //--op_code;
                //--user_id;  (image of whom)
                //--number_of_clusters;
                //output:
                //--op_code or none;
                //============================================================

                var userId = pr.ReadInt32();
                var numberOfCusters = pr.ReadInt32();
                var client = Clients.FirstOrDefault(x => x.Id == userId);

                if(client != null && client.MeetingId > 0)
                {
                    client.IsUsingCamera = true;
                    User_CameraFrame[userId] = new FrameBuilder(numberOfCusters);
                    //log.LogSuccess($"Frame builder for user: {userId} created with clusters size: {numberOfCusters}");

                    using var pb = new PacketBuilder();
                    pb.Write(OpCode.PARTICIPANT_TURNED_CAMERA_ON);
                    pb.Write(client.Id);

                    foreach(var participant in Clients.Where(x => x.MeetingId == client.MeetingId))
                    {
                        await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
                    }
                }
            }
            else if(opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE)
            {
                //============================================================
                //UPDATE CAMERA FRAME:
                //input:
                //--op_code;
                //--user_id;  (image of whom)
                //--cluster_position
                //--cluster_size;
                //--cluster;
                //output:
                //--FRAME_IS_CREATED:
                //-----broadcast_to_every_participant:
                //--------CREATE_FRAME:
                //---------------op_code;
                //---------------user_id;
                //---------------number_of_clusters;
                //--------UPDATE_FRAME:
                //---------------op_code;
                //---------------user_id;
                //---------------cluster_size;
                //---------------cluster;
                //============================================================

                var frameData = pr.ReadUserFrame();
                var frames = User_CameraFrame.GetValueOrDefault(frameData.UserId);

                if(frames != null)
                {
                    frames.AddFrame(frameData.Position, frameData.Data);

                    if(frames.IsFull)
                    {
                        log.LogSuccess("All frame received!");

                        //await BroadCastCameraFrameToParticipants(frameData.UserId, Clients, frames, token);
                        //var userMeeting = Clients.FirstOrDefault(x => x.Id == userId)?.MeetingId ?? -1;
                        var userMeeting = Clients.FirstOrDefault(x => x.Id == frameData.UserId)?.MeetingId ?? -1;

                        if(userMeeting >= 1)
                        {
                            var participants = Clients.Where(x => x.MeetingId == userMeeting);
                            await BroadcastFrameToParticipants(
                                OpCode.PARTICIPANT_CAMERA_FRAME_CREATE,
                                OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE,
                                frameData.UserId, participants, frames, token);
                        }
                    }
                }
            }
            else if(opCode == OpCode.PARTICIPANT_TURNED_CAMERA_OFF)
            {
                var userId = pr.ReadInt32();
                var client = Clients.FirstOrDefault(x => x.Id == userId);

                if(client != null && client.MeetingId > 0)
                {
                    using var pb = new PacketBuilder();
                    pb.Write(OpCode.PARTICIPANT_TURNED_CAMERA_OFF);
                    pb.Write(client.Id);

                    foreach (var participant in Clients.Where(x => x.MeetingId == client.MeetingId))
                    {
                        await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
                    }
                }
            }


            else if(opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON)
            {
                var userId = pr.ReadInt32();

                var user = Clients.FirstOrDefault(x => x.Id == userId);

                if(user != null && user.MeetingId > 0)
                {
                    var screenCapture = Meeting_ScreenCapture.GetValueOrDefault(user.MeetingId);

                    if(screenCapture != null)
                    {
                        using var pb = new PacketBuilder();
                        pb.Write(OpCode.ERROR);
                        pb.Write((byte)ErrorCode.SCREEN_CAPTURE_DOES_NOT_ALLOWED);
                        pb.Write("Screen capture is already taken!");
                        await udpServer.SendAsync(pb.ToArray(), user.IPAddress, token);
                    }
                    else
                    {
                        Meeting_ScreenCapture[user.MeetingId] = new(user.Id, new(0));
                        using var pb = new PacketBuilder();
                        pb.Write(OpCode.SUCCESS);
                        pb.Write((byte)ScsCode.SCREEN_DEMONSTRATION_ALLOWED);
                        pb.Write("Screen can be taken!");
                        await udpServer.SendAsync(pb.ToArray(), user.IPAddress, token);
                    }
                }
            }
            else if(opCode == OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME)
            {
                var userId = pr.ReadInt32();
                var numberOfCusters = pr.ReadInt32();
                var client = Clients.FirstOrDefault(x => x.Id == userId);

                if (client != null && client.MeetingId > 0 && Meeting_ScreenCapture[client.MeetingId] != null)
                {
                    Meeting_ScreenCapture[client.MeetingId] = new(userId, new(numberOfCusters));
                    log.LogSuccess($"Screen Frame builder for user: {userId} created with clusters size: {numberOfCusters}");

                    using var pb = new PacketBuilder();
                    pb.Write(OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON);
                    pb.Write(client.Id);

                    foreach (var participant in Clients.Where(x => x.MeetingId == client.MeetingId))
                    {
                        await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
                    }
                }
            }
            else if (opCode == OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME)
            {
                var frameData = pr.ReadUserFrame();
                var user = Clients.FirstOrDefault(x => x.Id == frameData.UserId);
                var screenFrame = Meeting_ScreenCapture.GetValueOrDefault(user?.MeetingId ?? -1);

                if(screenFrame != null)
                {
                    var frames = screenFrame.ScreenFrame;

                    if (frames != null)
                    {
                        frames.AddFrame(frameData.Position, frameData.Data);

                        if (frames.IsFull)
                        {
                            log.LogSuccess("All frame received!");

                            var userMeeting = Clients.FirstOrDefault(x => x.Id == frameData.UserId)?.MeetingId ?? -1;

                            if (userMeeting >= 1)
                            {
                                var participants = Clients.Where(x => x.MeetingId == userMeeting);
                                await BroadcastFrameToParticipants(
                                    OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME,
                                    OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME,
                                    frameData.UserId, participants, frames, token);
                            }
                        }
                    }
                }                
            }
            else if (opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF)
            {
                var userId = pr.ReadInt32();
                var client = Clients.FirstOrDefault(x => x.Id == userId);
                log.LogWarning($"Received command to notify screen capture stoped!");

                if (client != null && client.MeetingId > 0 && Meeting_ScreenCapture.GetValueOrDefault(client.MeetingId)?.UserId == userId)
                {
                    Meeting_ScreenCapture.Remove(client.MeetingId);
                    log.LogWarning($"Client with id: {userId} stoped demonstration in meeting: {client.MeetingId}!");

                    using var pb = new PacketBuilder();
                    pb.Write(OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF);
                    pb.Write(client.Id);

                    foreach (var participant in Clients.Where(x => x.MeetingId == client.MeetingId))
                    {
                        await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
                        log.LogWarning($"Client with id: {participant.Id} was notified!");
                    }
                }
            }

            else if(opCode == OpCode.PARTICIPANT_MESSAGE_SENT_EVERYONE)
            {
                var fromUserId = pr.ReadInt32();
                var toUserId = pr.ReadInt32();
                var message = pr.ReadString();
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
                var fromUser = pr.ReadInt32();
                var toUser = pr.ReadInt32();
                var numberOfClusters = pr.ReadInt32();
                var fileName = pr.ReadString();
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
                var frameData = pr.ReadUserFrame();
                var frames = User_FileBuilder.GetValueOrDefault(frameData.UserId);

                if (frames != null)
                {
                    frames.FrameBuilder.AddFrame(frameData.Position, frameData.Data);

                    if (frames.FrameBuilder.IsFull)
                    {
                        log.LogSuccess("All frame received!");

                        await BroadcastFileToParticipants(frames, token);
                    }
                }
            }


        }
        catch (Exception ex) 
        {
            log.LogError(ex.Message);
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
