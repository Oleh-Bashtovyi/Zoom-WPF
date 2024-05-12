using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows.Markup;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
using Zoom_Server.Net;
using Zoom_Server.Net.Codes;
using Zoom_Server.Net.Packets;
using Zoom_UI.Extensions;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.ClientServer;

public class UdpComunicator : OneProcessServer
{
    public class FileBuilder
    {
        public FrameBuilder FrameBuilder { get; set; }
        public string FileName { get; set; }
        public int FromUserId { get; set; }
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
    //========================================================================
    public event Action<UserModel>? OnUserCreated;
    public event Action<UserModel>? OnUserChangedName;
    public event Action<UserModel>? OnUserIdReceived;
    //RESPONSE
    //========================================================================
    public event Action<ErrorModel>? OnErrorReceived;
    public event Action<SuccessModel>? OnSuccessReceived;
    //MEETING CREATION
    //========================================================================
    public event Action<MeetingInfo>? OnMeetingCreated;
    public event Action<MeetingInfo>? OnUserJoinedMeeting_UsingCode;
    //PARTICIPATING
    //========================================================================
    public event Action<UserModel>? OnUser_JoinedMeeting;
    public event Action<UserModel>? OnUser_LeftMeeting;
    //CAMERA IMAGE
    //========================================================================
    public event Action<ImageFrame>? OnCameraFrameOfUserUpdated;
    public event Action<UserModel>? OnUser_TurnedCamera_ON;
    public event Action<UserModel>? OnUser_TurnedCamera_OFF;
    //SCREEN SHARE
    //========================================================================
    public event Action<ImageFrame>? OnScreenDemonstrationFrameOfUserUpdated;
    public event Action<UserModel>? OnUser_TurnedDemonstration_ON;
    public event Action<UserModel>? OnUser_TurnedDemonstration_OFF;
    //MESSAGES
    //========================================================================
    public event Action<MessageInfo>? OnMessageSent;
    //AUDION
    //========================================================================
    public event Action<UserModel>? OnUser_TurnedMicrophone_ON;
    public event Action<UserModel>? OnUser_TurnedMicrophone_OFF;
    public event Action<AudioFrame>? OnUser_SentAudioFrame;



    private BlockingCollection<byte[]> PacketsBuffer { get; } = new();


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








    public async Task SEND_MESSAGE(int fromUserId, int toUserId, string message)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_MESSAGE_SENT_EVERYONE);
        pb.Write(fromUserId);
        pb.Write(toUserId);
        pb.Write(message);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }



    public void SendJoinMeetingUsingCode(int meetingCode)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING);
        pb.Write(meetingCode);
        //log.LogSuccess($"Sending request for meeting joining. Meetingcode: {meetingCode}");
        PacketsBuffer.Add(pb.ToArray());
    }
    public void Send_UserJoinedMeeting(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_JOINED_MEETING, userId, meetingId);
    }
    public void Send_UserLeftMeeting(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_LEFT_MEETING, userId, meetingId);
    }
    public void Send_UserTurnCameraOn(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_CAMERA_ON, userId, meetingId);
    }
    public void Send_UserTurnCameraOff(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_CAMERA_OFF, userId, meetingId);
    }
    public void Send_CameraFrame(int userId, int meetingId, Bitmap bitmap)
    {
        var bytes = bitmap.AsByteArray();
        var clusters = bytes.AsClusters(32768);

        using (var ms = new MemoryStream())
        using (var pw = new BinaryWriter(ms))
        {
            pw.Write((byte)OpCode.PARTICIPANT_CAMERA_FRAME_CREATE);
            pw.Write(userId);
            pw.Write(meetingId);
            pw.Write(clusters.Count);
            var data = ms.ToArray();
            PacketsBuffer.Add(data);

            for (int i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];
                ms.Clear();
                pw.Write((byte)OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE);
                pw.Write(userId);
                pw.Write(meetingId);
                pw.Write(i);
                pw.Write(cluster.Length);
                pw.Write(cluster);
                PacketsBuffer.Add(ms.ToArray());
            }
        }
    }
    public void Send_UserTurnMicrophoneOn(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_MICROPHONE_ON, userId, meetingId);
    }
    public void Send_UserTurnMicrophoneOff(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_MICROPHONE_OFF, userId, meetingId);
    }
    public void Send_Audio(int userId, int meetingId, byte[] audio)
    {
        SendPacket(OpCode.PARTICIPANT_SENT_AUDIO, userId, meetingId, audio);
    }


    public void SEND_USER_TURN_SCREEN_CAPTURE_ON(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON, userId, meetingId);
    }
    public void SEND_USER_TURN_SCREEN_CAPTURE_OFF(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF, userId, meetingId);
    }
    /*    public async Task SEND_SCREEN_IMAGE(int userId, int meetingId, Bitmap bitmap)
        {
            var bytes = bitmap.AsByteArray();
            var clusters = bytes.AsClusters(32768);
            using var pw = new PacketBuilder();
            pw.Write(OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME);
            pw.Write(userId);
            pw.Write(meetingId);
            pw.Write(clusters.Count);
            var data = pw.ToArray();
            PacketsBuffer.Add(data);
            await Task.Delay(5);

            for (int i = 0; i < clusters.Count; i++)
            {
                pw.Clear();
                pw.Write((byte)OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME);
                pw.Write(userId);
                pw.Write(meetingId);
                pw.Write(i);
                pw.Write(clusters[i].Length);
                pw.Write(clusters[i]);
                PacketsBuffer.Add(pw.ToArray());    
                await Task.Delay(3);
            }
        }*/

    public void SEND_SCREEN_IMAGE(int userId, int meetingId, Bitmap bitmap)
    {
        var bytes = bitmap.AsByteArray();
        var clusters = bytes.AsClusters(32768);

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME);
            bw.Write(userId);
            bw.Write(meetingId);
            bw.Write(clusters.Count);
            PacketsBuffer.Add(ms.ToArray());

            for (int i = 0; i < clusters.Count; i++)
            {
                ms.Clear();
                bw.Write((byte)OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME);
                bw.Write(userId);
                bw.Write(meetingId);
                bw.Write(i);
                bw.Write(clusters[i].Length);
                bw.Write(clusters[i]);
                PacketsBuffer.Add(ms.ToArray());
            }
        }
    }





    private void SendPacket(OpCode code, int userId, int meetingId)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)code);
            bw.Write(userId);
            bw.Write(meetingId);
            PacketsBuffer.Add(ms.ToArray());
        }
    }
    private void SendPacket(OpCode code, int userId, int meetingId, byte[] data)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)code);
            bw.Write(userId);
            bw.Write(meetingId);
            bw.Write(data.Length);
            bw.Write(data);
            PacketsBuffer.Add(ms.ToArray());
        }
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










    protected async Task SendingProcess(CancellationToken token)
    {
        log.LogSuccess("Sending process started!");
        var exceptionCount = 0;

        while (true)
        {
            try
            {
                var packet = PacketsBuffer.Take(token);
                await _comunicator.SendAsync(packet, _serverEndPoint, token);
            }
            catch (Exception ex)
            {
                log.LogError("(SENDING PROCESS): " + ex.Message);
                exceptionCount++;   

                if(exceptionCount >= 10)
                {
                    log.LogError($"Too many errors occured in SENDING PROCESS!\nCount: {exceptionCount}");
                    break;
                }
            }
        }
    }






    protected override async Task Process(CancellationToken token)
    {
        Task.Run(() => SendingProcess(token));

        try
        {
            log.LogSuccess("Listener started!");

            while (true)
            {
                try
                {
                    var packet = await _comunicator.ReceiveAsync(token);
/*                    using var ms = new MemoryStream(packet.Buffer);
                    using var br = new BinaryReader(ms);
                    var opCode = (OpCode)br.ReadByte();*/
                    using var ms = new MemoryStream(packet.Buffer);
                    using var br = new PacketReader(ms);
                    var opCode = br.ReadOpCode();
                    //log.LogWarning($"Received op code: {opCode}");

                    //===================================================================
                    //----RESULTS
                    //===================================================================
                    if (opCode == OpCode.ERROR)
                    {
                        var code = br.ReadErrorCode();
                        var message = br.ReadString();
                        OnErrorReceived?.Invoke(new(code, message));
                    }
                    else if (opCode == OpCode.SUCCESS)
                    {
                        var code = (ScsCode)br.ReadByte();
                        var message = br.ReadString();
                        OnSuccessReceived?.Invoke(new(code, message));
                    }
                    //===================================================================
                    //----AUDIO
                    //===================================================================
                    else if(opCode == OpCode.PARTICIPANT_SENT_AUDIO)
                    {
                        var userId = br.ReadInt32();
                        var length = br.ReadInt32();
                        var data = br.ReadBytes(length);
                        //log.LogSuccess($"Audio received! User: {userId}, Length: {data.Length}");
                        OnUser_SentAudioFrame?.Invoke(new() { UserId = userId, Data = data });
                    }
                    else if(opCode == OpCode.PARTICIPANT_TURNED_MICROPHONE_ON)
                    {
                        var userId = br.ReadInt32();
                        OnUser_TurnedMicrophone_ON?.Invoke(new(userId, string.Empty));
                    }
                    else if (opCode == OpCode.PARTICIPANT_TURNED_MICROPHONE_OFF)
                    {
                        var userId = br.ReadInt32();
                        OnUser_TurnedMicrophone_OFF?.Invoke(new(userId, string.Empty));
                    }
                    //===================================================================
                    //----CAMERA
                    //===================================================================
                    else if(opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CREATE)
                    {
                        var userId = br.ReadInt32();
                        var framesCount = br.ReadInt32();
                        //log.LogWarning($"Received command to create camera frame! user:{userId}  clusters:{framesCount}");
                        User_CameraFrame[userId] = new FrameBuilder(framesCount);
                    }
                    else if(opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE)
                    {
                        var frameInfo = br.ReadUserFrame();
                        var frameBuilder = User_CameraFrame[frameInfo.UserId];
                        //log.LogWarning($"Recived camera frame cluster! position{frameInfo.Position}, userId:{frameInfo.UserId}");
                        frameBuilder.AddFrame(frameInfo.Position, frameInfo.Data);

                        if (frameBuilder.IsFull)
                        {
                            var image = frameBuilder.AsByteArray().AsBitmapImage();
                            //log.LogWarning($"Received full camera frame!");
                            OnCameraFrameOfUserUpdated?.Invoke(new (frameInfo.UserId, image));
                        }
                    }
                    else if (opCode == OpCode.PARTICIPANT_TURNED_CAMERA_ON)
                    {
                        var userId = br.ReadInt32();
                        OnUser_TurnedCamera_ON?.Invoke(new(userId, string.Empty));
                    }
                    else if(opCode == OpCode.PARTICIPANT_TURNED_CAMERA_OFF)
                    {
                        var userId = br.ReadInt32();
                        OnUser_TurnedCamera_OFF?.Invoke(new(userId, string.Empty));
                    }
                    //===================================================================
                    //----SCREEN
                    //===================================================================
                    else if (opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON)
                    {
                        var userId = br.ReadInt32();
                        OnUser_TurnedDemonstration_ON?.Invoke(new(userId, string.Empty));
                    }
                    else if (opCode == OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF)
                    {
                        var userId = br.ReadInt32();
                        OnUser_TurnedDemonstration_OFF?.Invoke(new(userId, string.Empty));
                    }
                    else if (opCode == OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME)
                    {
                        var userId = br.ReadInt32();
                        var framesCount = br.ReadInt32();
                        log.LogWarning($"Received command to create screen frame! user:{userId}  clusters:{framesCount}");
                        _screenCaptureBuilder = new FrameBuilder(framesCount);
                    }
                    else if (opCode == OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME)
                    {
                        var frameInfo = br.ReadUserFrame();
                        log.LogWarning($"Recived camera frame cluster! position{frameInfo.Position}, userId:{frameInfo.UserId}");
                        _screenCaptureBuilder.AddFrame(frameInfo.Position, frameInfo.Data);

                        if (_screenCaptureBuilder.IsFull)
                        {
                            var image = _screenCaptureBuilder.AsByteArray().AsBitmapImage();
                            log.LogWarning($"Received full camera frame!");
                            OnScreenDemonstrationFrameOfUserUpdated?.Invoke(new(frameInfo.UserId, image));
                        }
                    }
                    //===================================================================
                    //----PARTICIPATING
                    //===================================================================
                    else if(opCode == OpCode.PARTICIPANT_JOINED_MEETING)
                    {
                        var userId = br.ReadInt32();
                        var userName = br.ReadString();
                        OnUser_JoinedMeeting?.Invoke(new(userId, userName));
                    }
                    else if(opCode == OpCode.PARTICIPANT_LEFT_MEETING)
                    {
                        var userId = br.ReadInt32();
                        log.LogWarning($"User: {userId} leaving meeting!");
                        OnUser_LeftMeeting?.Invoke(new(userId, string.Empty));
                    }
                    else if(opCode == OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING)
                    {
                        var meetingId = br.ReadInt32();
                        OnUserJoinedMeeting_UsingCode?.Invoke(new(meetingId));
                    }
                    //===================================================================
                    //----MESSAGES
                    //===================================================================
                    else if (opCode == OpCode.PARTICIPANT_FILE_SEND_FRAME_CREATE)
                    {
                        var fromUser = br.ReadInt32();
                        var toUser = br.ReadInt32();
                        var numberOfClusters = br.ReadInt32();
                        var fileName = br.ReadString();


                        var fileBuilder = new FileBuilder();
                        fileBuilder.FileName = fileName;
                        fileBuilder.ToUserId = toUser;
                        fileBuilder.FromUserId = fromUser;
                        fileBuilder.FrameBuilder = new(numberOfClusters);
                        USer_FileBuilder[fromUser] = fileBuilder;
                    }
                    else if (opCode == OpCode.PARTICIPANT_FILE_SEND_FRAME_UPDATE)
                    {
                        var frameData = br.ReadUserFrame();
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
                    else if(opCode == OpCode.PARTICIPANT_MESSAGE_SENT_EVERYONE)
                    {
                        var fromUser = br.ReadInt32();
                        var toUser = br.ReadInt32();
                        var message = br.ReadString();
                        OnMessageSent?.Invoke(new(fromUser, toUser, message));
                    }
                    //===================================================================
                    //----GENERAL
                    //===================================================================
                    else if (opCode == OpCode.CREATE_MEETING)
                    {
                        var id = br.ReadInt32();
                        OnMeetingCreated?.Invoke(new(id));
                    }
                    else if (opCode == OpCode.CREATE_USER)
                    {
                        var id = br.ReadInt32();
                        var username = br.ReadString();
                        log.LogWarning($"Received new user! Id: {id} username: {username}");
                        OnUserCreated?.Invoke(new(id, username));
                    }
                    else if(opCode == OpCode.CHANGE_USER_NAME)
                    {
                        var userInfo = br.ReadUserInfo();
                        OnUserChangedName?.Invoke(new(userInfo.Id, userInfo.Username));
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
