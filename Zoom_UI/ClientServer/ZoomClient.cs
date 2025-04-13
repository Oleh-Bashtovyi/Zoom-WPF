using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
using Zoom_Server.Net;
using Zoom_Server.Net.Codes;
using Zoom_UI.Extensions;
using Zoom_UI.MVVM.Models;
using Zoom_UI.MVVM.Models.Frames;
using Zoom_UI.MVVM.ViewModels;
namespace Zoom_UI.ClientServer;

public class ZoomClient
{
    private int _port;
    private string _host;
    private ILogger log;

    //Process
    private readonly TimeSpan _serverTimeout;
    private UdpClient _comunicator;
    private IPEndPoint _serverEndPoint;
    private CancellationTokenSource? _cts = new();
    private Dictionary<int, FrameBuilder> User_CameraFrame = new();
    private FrameBuilder _screenCaptureBuilder = new(0);
    public bool IsRunning { get; private set; }
    public DateTime ServerLastPong { get; private set; }

    //RESPONSE
    //========================================================================
    public event Action<ErrorModel>? OnErrorReceived;
    public event Action<SuccessModel>? OnSuccessReceived;
    //PARTICIPATING
    //========================================================================
    public event Action<MeetingInfo>? OnCurrentUser_JoinedToMeeting;
    public event Action<UserModel>? OnUser_JoinedMeeting;
    public event Action<UserModel>? OnUser_LeftMeeting;
    //MESSAGES AND FILES
    //========================================================================
    public event Action<MessageInfo>? OnMessageSent;
    public event Action<MessageInfo>? OnFileUploaded;
    public event Action<FileFrame>? OnFilePartDownloaded;
    //CAMERA IMAGE
    //========================================================================
    public event Action<ImageFrame>? OnCameraFrameReceived;
    public event Action<UserModel>? OnUser_TurnedCamera_ON;
    public event Action<UserModel>? OnUser_TurnedCamera_OFF;
    //SCREEN SHARE
    //========================================================================
    public event Action<ImageFrame>? OnScreenCaptureFrameReceived;
    public event Action<UserModel>? OnUser_TurnedDemonstration_ON;
    public event Action<UserModel>? OnUser_TurnedDemonstration_OFF;
    //AUDIO
    //========================================================================
    public event Action<AudioFrame>? OnUser_SentAudioFrame;
    public event Action<UserModel>? OnUser_TurnedMicrophone_ON;
    public event Action<UserModel>? OnUser_TurnedMicrophone_OFF;






    public ZoomClient(string host, int port, ILogger logger, TimeSpan serverTimeout)
    {
        log = logger;
        _host = host;
        _port = port;
        _comunicator = new();
        _serverEndPoint = new(IPAddress.Parse(_host), _port);
        _serverTimeout = serverTimeout;
        log.LogSuccess("Listener initialized!");
    }



    #region Run\Stop
    public void Start()
    {
        if (!IsRunning)
        {
            _cts = new();
            var token = _cts.Token;

            Task.Run(() => ReceivingProcess(_cts.Token));
            Task.Run(() => PingProcess(_cts.Token));
            IsRunning = true;
        }
    }
    public void Stop()
    {
        if (IsRunning)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            IsRunning = false;
        }
    }
    #endregion







    public void Send_CreateMeeting(string username)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.CREATE_MEETING);
            bw.Write(username);
            _comunicator.Send(ms.ToArray(), _serverEndPoint);
        }
    }
    public void Send_JoinMeetingUsingCode(int meetingCode, string username)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING);
            bw.Write(username);
            bw.Write(meetingCode);
            _comunicator.Send(ms.ToArray(), _serverEndPoint);
        }
    }
    public void Send_UserLeftMeeting(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_LEFT_MEETING, userId, meetingId);
    }
    //=================================================================
    public void Send_UserTurnCamera_On(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_CAMERA_ON, userId, meetingId);
    }
    public void Send_UserTurnCamera_Off(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_CAMERA_OFF, userId, meetingId);
    }
    public void Send_CameraFrame(int userId, int meetingId, Bitmap bitmap)
    {
        var bytes = bitmap.AsByteArray();
        var clusters = bytes.AsClusters(32768);

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            for (int i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];

                if (i == 0)
                {
                    ms.Clear();
                    bw.Write((byte)OpCode.PARTICIPANT_CAMERA_FRAME_CREATE);
                    bw.Write(userId);
                    bw.Write(meetingId);
                    bw.Write(clusters.Count);

                    bw.Write(i);
                    bw.Write(cluster.Length);
                    bw.Write(cluster);

                    _comunicator.Send(ms.ToArray(), _serverEndPoint);
                }
                else
                {
                    ms.Clear();
                    bw.Write((byte)OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE);
                    bw.Write(userId);
                    bw.Write(meetingId);
                    bw.Write(i);
                    bw.Write(cluster.Length);
                    bw.Write(cluster);
                    _comunicator.Send(ms.ToArray(), _serverEndPoint);
                }
            }
        }
    }
    //=================================================================
    public void Send_UserTurnMicrophone_On(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_MICROPHONE_ON, userId, meetingId);
    }
    public void Send_UserTurnMicrophone_Off(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_MICROPHONE_OFF, userId, meetingId);
    }
    public void Send_Audio(int userId, int meetingId, byte[] audio)
    {
        SendPacket(OpCode.PARTICIPANT_SENT_AUDIO, userId, meetingId, audio);
    }
    //=================================================================
    public void Send_UserTurnScreenCapture_On(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_ON, userId, meetingId);
    }
    public void Send_UserTurnScreenCapture_Off(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_TURNED_SCREEN_CAPTURE_OFF, userId, meetingId);
    }
    public void Send_ScreenFrame(int userId, int meetingId, Bitmap bitmap)
    {
        var bytes = bitmap.AsByteArray();
        var clusters = bytes.AsClusters(64000);

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            for (int i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];

                if (i == 0)
                {
                    ms.Clear();
                    bw.Write((byte)OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME);
                    bw.Write(userId);
                    bw.Write(meetingId);
                    bw.Write(clusters.Count);

                    bw.Write(i);
                    bw.Write(cluster.Length);
                    bw.Write(cluster);

                    _comunicator.Send(ms.ToArray(), _serverEndPoint);
                }
                else
                {
                    ms.Clear();
                    bw.Write((byte)OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME);
                    bw.Write(userId);
                    bw.Write(meetingId);
                    bw.Write(i);
                    bw.Write(cluster.Length);
                    bw.Write(cluster);
                    _comunicator.Send(ms.ToArray(), _serverEndPoint);
                }
            }
        }
    }
    //=================================================================
    public void Send_MessageEveryone(int fromUserId, int meetingId, string message)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_MESSAGE_SEND_EVERYONE);
            bw.Write(fromUserId);
            bw.Write(meetingId);
            bw.Write(message);
            _comunicator.Send(ms.ToArray(), _serverEndPoint);
        }
    }
    public void Send_Message(int senderUserId, int receiverUserId, int meetingId, string message)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_MESSAGE_SEND);
            bw.Write(senderUserId);
            bw.Write(receiverUserId);
            bw.Write(meetingId);
            bw.Write(message);
            _comunicator.Send(ms.ToArray(), _serverEndPoint);
        }
    }
    //=================================================================
    public bool Send_FileEveryone(int meetingId, int senderId, string path, string fileId, CancellationToken token)
    {
        using (var file = File.OpenRead(path))
        {
            int bufferSize = 32768;
            int chunks = (int)Math.Ceiling((double)file.Length / bufferSize);

            for (var i = 0; i < chunks && !token.IsCancellationRequested; i++)
            {
                using (var ms = new MemoryStream(new byte[32900]))
                using (var bw = new BinaryWriter(ms))
                {
                    var data = new byte[bufferSize];
                    var bytesCount = file.Read(data, 0, bufferSize);

                    if (i + 1 == chunks)
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_LAST_EVERYONE);
                        bw.Write(senderId);
                        bw.Write(Path.GetFileName(path));
                    }
                    else
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_PART);
                    }

                    bw.Write(meetingId);
                    bw.Write(fileId);
                    bw.Write((long)i * bufferSize);   //cursor position
                    bw.Write(bytesCount);
                    bw.Write(data);
                    _comunicator.Send(ms.ToArray(), _serverEndPoint);
                }
            }
            if (token.IsCancellationRequested)
            {
                Send_FileDelete(meetingId, fileId);
                return true;
            }
            return true;
        }
    }
    public bool Send_File(int meetingId, int senderId, int receiverId,  string path, string fileId, CancellationToken token)
    {
        using (var file = File.OpenRead(path))
        {
            int bufferSize = 32768;
            int chunks = (int)Math.Ceiling((double)file.Length / bufferSize);

            for (var i = 0; i < chunks && !token.IsCancellationRequested; i++)
            {
                using (var ms = new MemoryStream(new byte[32900]))
                using (var bw = new BinaryWriter(ms))
                {
                    var data = new byte[bufferSize];
                    var bytesCount = file.Read(data, 0, bufferSize);

                    if (i + 1 == chunks)
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_LAST);
                        bw.Write(senderId);
                        bw.Write(receiverId);
                        bw.Write(Path.GetFileName(path));
                    }
                    else
                    {
                        bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_PART);
                    }

                    bw.Write(meetingId);
                    bw.Write(fileId);
                    bw.Write((long)(i * bufferSize));   //cursor position
                    bw.Write(bytesCount);
                    bw.Write(data);
                    _comunicator.Send(ms.ToArray(), _serverEndPoint);
                }
            }
            if (token.IsCancellationRequested)
            {
                Send_FileDelete(meetingId, fileId);
                return true;
            }
            return true;
        }
    }
    public void Send_FileDelete(int meetingId, string fileId)
    {
        using (var ms = new MemoryStream(16))
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_DELETE);
            bw.Write(meetingId);
            bw.Write(fileId);
            _comunicator.Send(ms.ToArray(), _serverEndPoint);
        }
    }
    //=================================================================
    public void DownloadFile(int meetingId,  string fileId, long byteIndex)
    {
        using (var ms = new MemoryStream(16))
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_DOWNLOAD);
            bw.Write(meetingId);
            bw.Write(fileId);
            bw.Write(byteIndex);
            _comunicator.Send(ms.ToArray(), _serverEndPoint);
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
            _comunicator.Send(ms.ToArray(), _serverEndPoint);
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
            _comunicator.Send(ms.ToArray(), _serverEndPoint);
        }
    }







    private async Task PingProcess(CancellationToken token)
    {
        try
        {
            var data = OpCode.PING.AsArray();

            ServerLastPong = DateTime.Now;

            while (!token.IsCancellationRequested)
            {
                if ((DateTime.Now - ServerLastPong) > _serverTimeout)
                {
                    MessageBox.Show("Server is not responding", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
                else
                {
                    _comunicator.Send(data, _serverEndPoint);
                }

                await Task.Delay(5000, token);
            }
        }
        catch(Exception ex)
        {
            log.LogError(ex.Message);
        }
    }



    protected async Task ReceivingProcess(CancellationToken token)
    {
        log.LogSuccess("Listener started!");

        while (true)
        {
            try
            {
                var packet = await _comunicator.ReceiveAsync(token);
                using var ms = new MemoryStream(packet.Buffer);
                using var br = new BinaryReader(ms);
                var opCode = (OpCode)br.ReadByte();


                //===================================================================
                //----PING-PONG
                //===================================================================
                if (opCode == OpCode.PING)
                {
                    log.LogSuccess($"ping to server!");
                    _comunicator.Send(OpCode.PONG.AsArray(), _serverEndPoint);
                }
                else if (opCode == OpCode.PONG)
                {
                    ServerLastPong = DateTime.Now;
                }
                //===================================================================
                //----RESULTS
                //===================================================================
                else if (opCode == OpCode.ERROR)
                {
                    var code = (ErrorCode)br.ReadByte();
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
                else if (opCode == OpCode.PARTICIPANT_SENT_AUDIO)
                {
                    var userId = br.ReadInt32();
                    var length = br.ReadInt32();
                    var data = br.ReadBytes(length);
                    OnUser_SentAudioFrame?.Invoke(new(userId, data));
                }
                else if (opCode == OpCode.PARTICIPANT_TURNED_MICROPHONE_ON)
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
                else if (opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CREATE)
                {
                    var userId = br.ReadInt32();
                    var framesCount = br.ReadInt32();

                    var framePosition = br.ReadInt32();
                    var dataLength = br.ReadInt32();
                    var data = br.ReadBytes(dataLength);

                    if (framesCount == 1)
                    {
                        var image = data.AsBitmapImage();
                        OnCameraFrameReceived?.Invoke(new(userId, image));
                    }
                    else
                    {
                        var framBuilder = new FrameBuilder(framesCount);
                        framBuilder.AddFrame(framePosition, data);
                        User_CameraFrame[userId] = framBuilder;
                    }
                }
                else if (opCode == OpCode.PARTICIPANT_CAMERA_FRAME_CLUESTER_UPDATE)
                {
                    var userId = br.ReadInt32();
                    var position = br.ReadInt32();
                    var clusterSize = br.ReadInt32();
                    var data = br.ReadBytes(clusterSize);
                    var frameBuilder = User_CameraFrame[userId];
                    frameBuilder.AddFrame(position, data);

                    if (frameBuilder.IsFull)
                    {
                        var image = frameBuilder.AsByteArray().AsBitmapImage();
                        OnCameraFrameReceived?.Invoke(new(userId, image));
                    }
                }
                else if (opCode == OpCode.PARTICIPANT_TURNED_CAMERA_ON)
                {
                    var userId = br.ReadInt32();
                    OnUser_TurnedCamera_ON?.Invoke(new(userId, string.Empty));
                }
                else if (opCode == OpCode.PARTICIPANT_TURNED_CAMERA_OFF)
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

                    var framePosition = br.ReadInt32();
                    var dataLength = br.ReadInt32();
                    var data = br.ReadBytes(dataLength);

                    if (framesCount == 1)
                    {
                        var image = data.AsBitmapImage();
                        OnScreenCaptureFrameReceived?.Invoke(new(userId, image));
                    }
                    else
                    {
                        var framBuilder = new FrameBuilder(framesCount);
                        framBuilder.AddFrame(framePosition, data);
                        _screenCaptureBuilder = framBuilder;
                    }

                    //log.LogWarning($"Received request to create screen frame of size: {framesCount} for user: {userId}");
                }
                else if (opCode == OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME)
                {
                    var userId = br.ReadInt32();
                    var position = br.ReadInt32();
                    var clusterSize = br.ReadInt32();
                    var data = br.ReadBytes(clusterSize);
                    _screenCaptureBuilder.AddFrame(position, data);
                    //log.LogWarning($"RECEIVED FRAME: {position}, IS FULL: {_screenCaptureBuilder.IsFull}, USER: {userId}");
                    if (_screenCaptureBuilder.IsFull)
                    {
                        var image = _screenCaptureBuilder.AsByteArray().AsBitmapImage();
                        OnScreenCaptureFrameReceived?.Invoke(new(userId, image));
                    }
                }
                //===================================================================
                //----PARTICIPATING
                //===================================================================
                else if (opCode == OpCode.PARTICIPANT_JOINED_MEETING)
                {
                    var userId = br.ReadInt32();
                    var userName = br.ReadString();
                    OnUser_JoinedMeeting?.Invoke(new(userId, userName));
                }
                else if (opCode == OpCode.PARTICIPANT_LEFT_MEETING)
                {
                    var userId = br.ReadInt32();
                    OnUser_LeftMeeting?.Invoke(new(userId, string.Empty));
                }


                else if (opCode == OpCode.USER_CONNECTED_TO_MEETING)
                {
                    var meetingId = br.ReadInt32();
                    var isScreenCaptureActive = br.ReadBoolean();
                    var screenDemonstratorId = -1;

                    if(isScreenCaptureActive)
                    {
                        screenDemonstratorId = br.ReadInt32();
                    }

                    var currentParticipantId = br.ReadInt32();
                    var participantsCount = br.ReadInt32();
                    var participants = new List<UserViewModel>();

                    for (int i = 0; i < participantsCount; i++)
                    {
                        var participantId = br.ReadInt32();
                        var participantName = br.ReadString();
                        var isMicrophonOn = br.ReadBoolean();
                        var isCameraOn = br.ReadBoolean();
                        var participant = new UserViewModel()
                        {
                            Id = participantId,
                            Username = participantName,
                            IsCameraOn = isCameraOn,
                            IsMicrophoneOn = isMicrophonOn,
                        };
                        participants.Add(participant);
                    }

                    var currentParticipant = participants.FirstOrDefault(x => x.Id == currentParticipantId);

                    if (currentParticipant == null)
                    {
                        throw new Exception("Current participants is not defined!");
                    }
                    currentParticipant.IsCurrentUser = true;

                    UserViewModel? screenDemonstrator = null;

                    if (isScreenCaptureActive)
                    {
                        screenDemonstrator = participants.FirstOrDefault(x => x.Id == screenDemonstratorId);
                    }

                    var meetingInfo = new MeetingInfo(meetingId, currentParticipant, participants, screenDemonstrator);
                    OnCurrentUser_JoinedToMeeting?.Invoke(meetingInfo);
                }


                //===================================================================
                //----FILES
                //===================================================================
                else if (opCode == OpCode.PARTICIPANT_SEND_FILE_UPLOADED)
                {
                    var senderName = br.ReadString();
                    var fileName = br.ReadString();
                    var fileSize = br.ReadInt64();
                    var fileId = br.ReadString();
                    var fileModel = new FileModel(fileId, fileName, fileSize);
                    OnFileUploaded?.Invoke(new(senderName, "You", fileModel));
                }
                else if (opCode == OpCode.PARTICIPANT_SEND_FILE_UPLOADED_EVERYONE)
                {
                    var senderName = br.ReadString();
                    var fileName = br.ReadString();
                    var fileSize = br.ReadInt64();
                    var idName = br.ReadString();
                    var fileModel = new FileModel(idName, fileName, fileSize);
                    OnFileUploaded?.Invoke(new(senderName, "Everyone", fileModel));
                }
                else if (opCode == OpCode.PARTICIPANT_SEND_FILE_DOWNLOAD)
                {
                    var fileId = br.ReadString();
                    var dataSize = br.ReadInt32();
                    var data = br.ReadBytes(dataSize);
                    OnFilePartDownloaded?.Invoke(new(fileId, data));
                }
                //===================================================================
                //----MESSAGES
                //===================================================================
                else if (opCode == OpCode.PARTICIPANT_MESSAGE_SEND_EVERYONE)
                {
                    var fromUser = br.ReadString();
                    var message = br.ReadString();
                    OnMessageSent?.Invoke(new(fromUser, "Everyone", message));
                }
                else if (opCode == OpCode.PARTICIPANT_MESSAGE_SEND)
                {
                    var fromUser = br.ReadString();
                    var toUser = br.ReadString();
                    var message = br.ReadString();
                    OnMessageSent?.Invoke(new(fromUser, toUser, message));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }
    }
}
