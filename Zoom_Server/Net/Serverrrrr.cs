using System.Net;
using System.Net.Sockets;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
namespace Zoom_Server.Net;
#pragma warning disable CS8618 





internal class UdpServer
{
    private int _port;
    private string _host;
    private ILogger log;
    private UdpClient udpServer;
    private Task _udpRunningProcess;

    //Collections
    private HashSet<int> MeetingsIds { get; } = new();
    private List<Client> Clients { get; } = new();
    private Dictionary<int, FrameBuilder> User_CameraFrame { get; } = new();


    //Process
    private CancellationTokenSource _cancellationTokenSource { get; set; } = new();
    public bool IsRunning => _udpRunningProcess != null && !_udpRunningProcess.IsCompleted;






    public UdpServer(string host, int port, ILogger logger)
    {
        _host = host;
        _port = port;
        log = logger;
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
        _udpRunningProcess = Task.Run(() => TcpListeningProcess(_cancellationTokenSource.Token));
    }
    internal void Stop()
    {
        if (!IsRunning)
        {
            throw new Exception("Server is not running!");
        }

        _cancellationTokenSource?.Cancel();
        _udpRunningProcess = null;
    }
    #endregion


    private async Task TcpListeningProcess(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var receivedResult = await udpServer.ReceiveAsync(token);
                log.LogSuccess($"Server received some data! Data size: {receivedResult.Buffer.Length} bytes");
                await HandleRequest(receivedResult, token);
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
        using var ms = new MemoryStream(asyncResult.Buffer);
        using var br = new BinaryReader(ms);

        try
        {
            var opCode = (OpCode)br.ReadByte();

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
                var userName = br.ReadString();
                var client = new Client(asyncResult.RemoteEndPoint, userName);
                Clients.Add(client);

                using var repsponse_ms = new MemoryStream();
                using var bw = new BinaryWriter(repsponse_ms);

                bw.Write(OpCode.CreateUser.AsByte());    //Op_code
                bw.Write(client.Id);                     //User Id
                bw.Write(client.Username);               //Username
                await udpServer.SendAsync(repsponse_ms.ToArray(), asyncResult.RemoteEndPoint, token);
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

                using var repsponse_ms = new MemoryStream();
                using var bw = new BinaryWriter(repsponse_ms);

                bw.Write(OpCode.CreateMeeting.AsByte());   //opcode
                bw.Write(newMeeting.ToString());           //meeting id
                await udpServer.SendAsync(repsponse_ms.ToArray(), asyncResult.RemoteEndPoint, token);
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
                var meetingCode = br.ReadInt32();

                if(MeetingsIds.Contains(meetingCode))
                {
                    using var repsponse_ms = new MemoryStream();
                    using var bw = new BinaryWriter(repsponse_ms);

                    bw.Write(OpCode.Participant_JoinMeetingUsingCode.AsByte());
                    await udpServer.SendAsync(repsponse_ms.ToArray(), asyncResult.RemoteEndPoint, token);
                }
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
                //--op_code;
                //============================================================

                var userId = br.ReadInt32();
                var numberOfCusters = br.ReadInt32();

                if(Clients.Any(x => x.Id == userId))
                {
                    User_CameraFrame[userId] = new FrameBuilder(numberOfCusters);
                    log.LogSuccess($"Frame builder for user: {userId} created with clusters size: {numberOfCusters}");
                    var response = new byte[] { OpCode.Participant_CameraFrame_Create.AsByte() };
                    await udpServer.SendAsync(response, asyncResult.RemoteEndPoint, token);
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

                var userId = br.ReadInt32();
                var position = br.ReadInt32();
                var clusterSize = br.ReadInt32();
                var cluster = br.ReadBytes(clusterSize);
                var frames = User_CameraFrame.GetValueOrDefault(userId);

                if(frames != null)
                {
                    frames.AddFrame(position, cluster);

                    if(frames.IsFull)
                    {
                        var userMeeting = Clients.FirstOrDefault(x => x.Id == userId)?.MeetingId ?? -1;

                        if(MeetingsIds.Contains(userMeeting))
                        {
                            await BroadCastCameraFrameToParticipants(userId, userMeeting, frames, token);
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



    private async Task BroadCastCameraFrameToParticipants(int userId, int meetingId, FrameBuilder builder, CancellationToken token)
    {
        var participants = Clients.Where(x => x.MeetingId == meetingId);
        
        if(!participants.Any())
        {
            return;
        }

        var frames = builder.GetFrames();

        foreach (var participant in participants)
        {
            using var repsponse_ms = new MemoryStream();
            using var bw = new BinaryWriter(repsponse_ms);
            bw.Write(OpCode.Participant_CameraFrame_Create.AsByte());
            bw.Write(userId);
            bw.Write(frames.Count());
            await udpServer.SendAsync(repsponse_ms.ToArray(), participant.IPAddress, token);


        }
    }
}
#pragma warning restore CS8618
