using System.Net;
using System.Net.Sockets;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
namespace Zoom_Server.Net;
#pragma warning disable CS8618 


internal class UdpServer : OneProcessServer
{
    private UdpClient udpServer;


    //Collections
    private HashSet<int> MeetingsIds { get; } = new();
    private List<Client> Clients { get; } = new();
    private Dictionary<int, FrameBuilder> User_CameraFrame { get; } = new();




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



    private async Task HandleRequest(UdpReceiveResult asyncResult, CancellationToken token)
    {
        using var pr = new PacketReader(new MemoryStream(asyncResult.Buffer));

        try
        {
            var opCode = pr.ReadOpCode();

            if(opCode == OpCode.CreateUser)
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
                pb.Write(OpCode.CreateUser.AsByte());
                pb.Write_UserInfo(client.Id, userName);
                log.LogWarning($"Sending new user info: id:{client.Id} username:{client.Username}");
                await udpServer.SendAsync(pb.ToArray(), asyncResult.RemoteEndPoint, token);
            }
            else if(opCode == OpCode.CreateMeeting)
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
                pb.Write(OpCode.CreateMeeting.AsByte());   
                pb.Write(newMeeting);                      
                log.LogWarning($"Sending new meeting info: id:{newMeeting}");
                await udpServer.SendAsync(pb.ToArray(), asyncResult.RemoteEndPoint, token);
            }
            else if(opCode == OpCode.Participant_JoinMeetingUsingCode)
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
                    pw.Write(OpCode.Participant_JoinMeetingUsingCode);
                    pw.Write(meetingCode);
                    log.Log($"Somebody asked to enter meeting room! Room id:{meetingCode}");
                    await udpServer.SendAsync(pw.ToArray(), asyncResult.RemoteEndPoint, token);
                }
            }
            else if(opCode == OpCode.Participant_JoinedMeeting)
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
            else if(opCode == OpCode.Participant_CameraFrame_Create)
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

                if(Clients.Any(x => x.Id == userId))
                {
                    User_CameraFrame[userId] = new FrameBuilder(numberOfCusters);
                    log.LogSuccess($"Frame builder for user: {userId} created with clusters size: {numberOfCusters}");
                    //var response = new byte[] { OpCode.Participant_CameraFrame_Create.AsByte() };
                    //await udpServer.SendAsync(response, asyncResult.RemoteEndPoint, token);
                }
            }
            else if(opCode == OpCode.Participant_CameraFrame_Update)
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
                            await BroadCastCameraFrameToParticipants(frameData.UserId, participants, frames, token);
                        }
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
                pb.Write(OpCode.Participant_JoinedMeeting);
                pb.Write_UserInfo(participant_2.Id, participant_2.Username);
                log.LogSuccess($"Broadcating joining info about user:{participant_2.Id} to user: {participant.Id}");
                await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
            }
        }
    }




    private async Task BroadCastCameraFrameToParticipants(int userId, IEnumerable<Client> participants, FrameBuilder builder, CancellationToken token)
    {
        var frames = builder.GetFrames();
        using var pb = new PacketBuilder();

        log.LogWarning("Process of sending frame begun!");
        foreach (var participant in participants)
        {
            pb.Clear();
            pb.Write(OpCode.Participant_CameraFrame_Create.AsByte());
            pb.Write(userId);
            pb.Write(frames.Count());
            await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);

            for (int i = 0; i < frames.Length; i++)
            {
                pb.Clear();
                pb.Write(OpCode.Participant_CameraFrame_Update.AsByte());
                pb.Write_UserFrame(userId, i, frames[i]);
                await udpServer.SendAsync(pb.ToArray(), participant.IPAddress, token);
            }

            log.LogSuccess($"Frame was sent to user: id:{participant.Id} name:{participant.Username}");
        }
    }
}
#pragma warning restore CS8618
