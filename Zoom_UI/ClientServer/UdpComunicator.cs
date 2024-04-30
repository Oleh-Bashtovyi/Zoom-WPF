using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
using Zoom_Server.Net;
using Zoom_UI.Extensions;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.ClientServer;

public class UdpComunicator : OneProcessServer
{
    public class FileBuilder
    {
        public FrameBuilder FrameBuilder { get; set; }
        public string FileName { get; set; }
        public int FromUserId {  get; set; }
        public int ToUserId { get; set; }
    }


    public class AudioFrame
    {
        public int UserId { get; set; }
        public byte[] Data { get; set; }
    }


    private UdpClient _comunicator;
    private IPEndPoint _serverEndPoint;
    private Dictionary<int, FrameBuilder> User_CameraFrame = new();
    private Dictionary<int, FileBuilder> USer_FileBuilder { get; } = new();

    private FrameBuilder _screenCaptureBuilder = new(0);



    //GENERAL
    public event Action<UserModel>? OnUserCreated;
    public event Action<UserModel>? OnUserChangedName;
    public event Action<UserModel>? OnUserIdReceived;
    //RESPONSE
    public event Action<ErrorModel>? OnErrorReceived;
    public event Action<SuccessModel>? OnSuccessReceived;
    //MEETING CREATION
    public event Action<MeetingInfo>? OnMeetingCreated;
    public event Action<MeetingInfo>? OnUserJoinedMeeting_UsingCode;
    //PARTICIPATING
    public event Action<UserModel>? OnUser_JoinedMeeting;
    public event Action<UserModel>? OnUser_LeftMeeting;
    //CAMERA IMAGE
    public event Action<ImageFrame>? OnCameraFrameOfUserUpdated;
    public event Action<UserModel>? OnUser_TurnedCamera_ON;
    public event Action<UserModel>? OnUser_TurnedCamera_OFF;
    //SCREEN SHARE
    public event Action<ImageFrame>? OnScreenDemonstrationFrameOfUserUpdated;
    public event Action<UserModel>? OnUser_TurnedDemonstration_ON;
    public event Action<UserModel>? OnUser_TurnedDemonstration_OFF;
    //MESSAGES
    public event Action<MessageInfo>? OnMessageSent;
    //AUDION
    public event Action<UserModel>? OnUser_TurnedMicrophone_ON;
    public event Action<UserModel>? OnUser_TurnedMicrophone_OFF;
    public event Action<AudioFrame>? OnUser_SentAudioFrame;




    public UdpComunicator(string host, int port, ILogger logger) : base(host, port, logger)
    {
        _comunicator = new();
        _comunicator.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        _serverEndPoint = new(IPAddress.Parse(_host), _port);
        log.LogSuccess("Listener initialized!");
    }


    public async Task SEND_CREATE_USER(string username)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.CREATE_USER);
        pb.Write(username);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }
    public async Task SEND_CHANGE_NAME(int userId, string newUSername)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.CHANGE_USER_NAME);
        pb.Write(userId);
        pb.Write(newUSername);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }
    public async Task SEND_CREATE_MEETING()
    {
        await _comunicator.SendAsync(OpCode.CREATE_MEETING.AsArray(), _serverEndPoint);
    }
    public async Task SEND_CAMERA_FRAME(int fromUser_id, Bitmap bitmap)
    {
        var bytes = bitmap.AsByteArray();
        var clusters = bytes.AsClusters(32768);
        using var pw = new PacketBuilder();
        pw.Write(OpCode.PARTICIPANT_CAMERA_FRAME_CREATE);
        pw.Write(fromUser_id);
        pw.Write(clusters.Count);
        var data = pw.ToArray();
        await _comunicator.SendAsync(data, _serverEndPoint);
        await Task.Delay(25);

        for (int i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            pw.Clear();
            pw.Write(OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE);
            pw.Write_UserFrame(fromUser_id, i, cluster);
            data = pw.ToArray();
            await _comunicator.SendAsync(data, _serverEndPoint);
            await Task.Delay(5);
        }
    }
    public async Task SEND_MESSAGE(int fromUserId, int toUserId, string message)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_MESSAGE_SENT_EVERYONE);
        pb.Write(fromUserId); 
        pb.Write(toUserId); 
        pb.Write(message);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }
    public async Task SEND_JOIN_MEETING_USING_CODE(int meetingCode)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING);
        pb.Write(meetingCode);
        log.LogSuccess($"Sending request for meeting joining. Meetingcode: {meetingCode}");
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }
    public async Task SEND_USER_JOINED_MEETING(int userId, int meetingCode)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_JOINED_MEETING);
        pb.Write(userId);
        pb.Write(meetingCode);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }
    public async Task SEND_USER_LEAVE_MEETING(int userId, string username)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_LEFT_MEETING);
        pb.Write(userId);
        pb.Write(username);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }



    public async Task SEND_REQUEST_FOR_SCREEN_DEMONSTRATION(int userId)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON);
        pb.Write(userId);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }



    public async Task SEND_SCREEN_IMAGE(int userId, Bitmap bitmap)
    {
        var bytes = bitmap.AsByteArray();
        var clusters = bytes.AsClusters(32768);
        using var pw = new PacketBuilder();
        pw.Write(OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME);
        pw.Write(userId);
        pw.Write(clusters.Count);
        var data = pw.ToArray();
        await _comunicator.SendAsync(data, _serverEndPoint);
        //await Task.Delay(25);

        for (int i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            pw.Clear();
            pw.Write(OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME);
            pw.Write_UserFrame(userId, i, cluster);
            data = pw.ToArray();
            await _comunicator.SendAsync(data, _serverEndPoint);
            await Task.Delay(5);
        }
    }






    public async Task SEND_USER_TURN_OFF_DEMONSTRATION(int userId)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF);
        pb.Write(userId);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }



    public async Task SEND_USER_TURN_OFF_CAMERA(int userId)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_TURNED_CAMERA_OFF);
        pb.Write(userId);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }



    public async Task SEND_FILE(int fromUserId, int toUserId, string filePath)
    {
        var clusters = File.ReadAllBytes(filePath).AsClusters(32768);
        using var pw = new PacketBuilder();
        pw.Write(OpCode.PARTICIPANT_FILE_SEND_FRAME_CREATE);
        pw.Write(fromUserId);
        pw.Write(toUserId);
        pw.Write(clusters.Count);
        pw.Write(Path.GetFileName(filePath));
        var data = pw.ToArray();
        await _comunicator.SendAsync(data, _serverEndPoint);
        await Task.Delay(25);

        for (int i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            pw.Clear();
            pw.Write(OpCode.PARTICIPANT_FILE_SEND_FRAME_UPDATE);
            pw.Write_UserFrame(fromUserId, i, cluster);
            data = pw.ToArray();
            await _comunicator.SendAsync(data, _serverEndPoint);
            await Task.Delay(5);
        }
    }



    public async Task SEND_AUDIO(int fromUserId, byte[] data)
    {
        using var pw = new PacketBuilder();
        pw.Write(OpCode.PARTICIPANT_SENT_AUDIO);
        pw.Write(fromUserId);
        pw.Write(data.Length);
        pw.Write(data);
        await _comunicator.SendAsync(pw.ToArray(), _serverEndPoint);
    }





    protected override async Task Process(CancellationToken token)
    {
        try
        {
            log.LogSuccess("Listener started!");
            while (true)
            {
                try
                {
                    //await Task.Delay(2000);
                    log.LogSuccess("Waiting for packets...");
                    var packet = await _comunicator.ReceiveAsync(token);
                    var pr = new PacketReader(new MemoryStream(packet.Buffer));
                    var opCode = pr.ReadOpCode();

                    log.LogWarning($"Received op code: {opCode}");

                    if(opCode == OpCode.ERROR)
                    {
                        var code = pr.ReadErrorCode();
                        var message = pr.ReadString();
                        OnErrorReceived?.Invoke(new(code, message));
                    }
                    if (opCode == OpCode.SUCCESS)
                    {
                        var code = (ScsCode)pr.ReadByte();
                        var message = pr.ReadString();
                        OnSuccessReceived?.Invoke(new(code, message));
                    }

                    else if(opCode == OpCode.PARTICIPANT_SENT_AUDIO)
                    {
                        var userId = pr.ReadInt32();
                        var length = pr.ReadInt32();
                        var data = pr.ReadBytes(length);
                        OnUser_SentAudioFrame?.Invoke(new() { UserId = userId, Data = data });
                    }

                    else if (opCode == OpCode.PARTICIPANT_FILE_SEND_FRAME_CREATE)
                    {
                        var fromUser = pr.ReadInt32();
                        var toUser = pr.ReadInt32();
                        var numberOfClusters = pr.ReadInt32();
                        var fileName = pr.ReadString();


                        var fileBuilder = new FileBuilder();
                        fileBuilder.FileName = fileName;
                        fileBuilder.ToUserId = toUser;
                        fileBuilder.FromUserId = fromUser;
                        fileBuilder.FrameBuilder = new(numberOfClusters);
                        USer_FileBuilder[fromUser] = fileBuilder;
                    }
                    else if (opCode == OpCode.PARTICIPANT_FILE_SEND_FRAME_UPDATE)
                    {
                        var frameData = pr.ReadUserFrame();
                        var frames = USer_FileBuilder.GetValueOrDefault(frameData.UserId);

                        if (frames != null)
                        {
                            frames.FrameBuilder.AddFrame(frameData.Position, frameData.Data);

                            if (frames.FrameBuilder.IsFull)
                            {
                                var content = new FIleModel()
                                {
                                    FileName = frames.FileName,
                                    Data = frames.FrameBuilder.AsByteArray()
                                };
                                var mes = new MessageInfo(frames.FromUserId, frames.ToUserId, content);

                                OnMessageSent?.Invoke(mes);
                            }
                        }
                    }





                    else if(opCode == OpCode.PARTICIPANT_TURNED_CAMERA_ON)
                    {
                        var userId = pr.ReadInt32();
                        OnUser_TurnedCamera_ON?.Invoke(new(userId, string.Empty));
                    }
                    else if(opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON)
                    {
                        var userId = pr.ReadInt32();
                        OnUser_TurnedDemonstration_ON?.Invoke(new(userId, string.Empty));
                    }
                    else if (opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF)
                    {
                        var userId = pr.ReadInt32();
                        OnUser_TurnedDemonstration_OFF?.Invoke(new(userId, string.Empty));
                    }
                    else if (opCode == OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME)
                    {
                        var userId = pr.ReadInt32();
                        var framesCount = pr.ReadInt32();
                        log.LogWarning($"Received command to create screen frame! user:{userId}  clusters:{framesCount}");
                        _screenCaptureBuilder = new FrameBuilder(framesCount);
                    }
                    else if (opCode == OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME)
                    {
                        var frameInfo = pr.ReadUserFrame();
                        log.LogWarning($"Recived camera frame cluster! position{frameInfo.Position}, userId:{frameInfo.UserId}");
                        _screenCaptureBuilder.AddFrame(frameInfo.Position, frameInfo.Data);

                        if (_screenCaptureBuilder.IsFull)
                        {
                            var image = _screenCaptureBuilder.AsByteArray().AsBitmapImage();
                            log.LogWarning($"Received full camera frame!");
                            OnScreenDemonstrationFrameOfUserUpdated?.Invoke(new(frameInfo.UserId, image));
                        }
                    }

                    else if(opCode == OpCode.PARTICIPANT_TURNED_CAMERA_OFF)
                    {
                        var userId = pr.ReadInt32();
                        OnUser_TurnedCamera_OFF?.Invoke(new(userId, string.Empty));
                    }
                    else if (opCode == OpCode.CREATE_MEETING)
                    {
                        var id = pr.ReadInt32();
                        OnMeetingCreated?.Invoke(new(id));
                    }
                    else if (opCode == OpCode.CREATE_USER)
                    {
                        var id = pr.ReadInt32();
                        var username = pr.ReadString();
                        log.LogWarning($"Received new user! Id: {id} username: {username}");
                        OnUserCreated?.Invoke(new(id, username));
                    }
                    else if(opCode == OpCode.PARTICIPANT_LEFT_MEETING)
                    {
                        var userInfo = pr.ReadUserInfo();
                        OnUser_LeftMeeting?.Invoke(new(userInfo.Id, userInfo.Username));
                    }
                    else if(opCode == OpCode.CHANGE_USER_NAME)
                    {
                        var userInfo = pr.ReadUserInfo();
                        OnUserChangedName?.Invoke(new(userInfo.Id, userInfo.Username));
                    }
                    else if(opCode == OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING)
                    {
                        var meetingId = pr.ReadInt32();
                        OnUserJoinedMeeting_UsingCode?.Invoke(new(meetingId));
                    }
                    else if(opCode == OpCode.PARTICIPANT_JOINED_MEETING)
                    {
                        var userId = pr.ReadInt32();
                        var userName = pr.ReadString();
                        OnUser_JoinedMeeting?.Invoke(new(userId, userName));
                    }
                    else if(opCode == OpCode.PARTICIPANT_MESSAGE_SENT_EVERYONE)
                    {
                        var fromUser = pr.ReadInt32();
                        var toUser = pr.ReadInt32();
                        var message = pr.ReadString();
                        OnMessageSent?.Invoke(new(fromUser, toUser, message));
                    }
                    else if(opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CREATE)
                    {
                        var userId = pr.ReadInt32();
                        var framesCount = pr.ReadInt32();
                        log.LogWarning($"Received command to create camera frame! user:{userId}  clusters:{framesCount}");
                        User_CameraFrame[userId] = new FrameBuilder(framesCount);
                    }
                    else if(opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE)
                    {
                        var frameInfo = pr.ReadUserFrame();
                        var frameBuilder = User_CameraFrame[frameInfo.UserId];
                        log.LogWarning($"Recived camera frame cluster! position{frameInfo.Position}, userId:{frameInfo.UserId}");
                        frameBuilder.AddFrame(frameInfo.Position, frameInfo.Data);

                        if (frameBuilder.IsFull)
                        {
                            var image = frameBuilder.AsByteArray().AsBitmapImage();
                            log.LogWarning($"Received full camera frame!");
                            OnCameraFrameOfUserUpdated?.Invoke(new (frameInfo.UserId, image));
                        }
                    }
                }
                catch(OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex.Message);
        }
    }
}
