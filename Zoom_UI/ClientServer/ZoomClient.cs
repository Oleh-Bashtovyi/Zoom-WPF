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
namespace Zoom_UI.ClientServer;

public class ZoomClient : OneProcessServer
{
    private UdpClient _comunicator;
    private IPEndPoint _serverEndPoint;
    private Dictionary<int, FrameBuilder> User_CameraFrame = new();
    private FrameBuilder _screenCaptureBuilder = new(0);
    private BlockingCollection<byte[]> SendingBuffer { get; } = new();



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
    public event Action<ImageFrame>? OnCameraFrameReceived;
    public event Action<UserModel>? OnUser_TurnedCamera_ON;
    public event Action<UserModel>? OnUser_TurnedCamera_OFF;
    //SCREEN SHARE
    //========================================================================
    public event Action<ImageFrame>? OnScreenCaptureFrameReceived;
    public event Action<UserModel>? OnUser_TurnedDemonstration_ON;
    public event Action<UserModel>? OnUser_TurnedDemonstration_OFF;
    //MESSAGES
    //========================================================================
    public event Action<MessageInfo>? OnMessageSent;
    //FILES
    //========================================================================
    public event Action<MessageInfo>? OnFileUploaded;
    public event Action<FileFrame>? OnFilePartDownloaded;
    //AUDION
    //========================================================================
    public event Action<UserModel>? OnUser_TurnedMicrophone_ON;
    public event Action<UserModel>? OnUser_TurnedMicrophone_OFF;
    public event Action<AudioFrame>? OnUser_SentAudioFrame;



    private TimeSpan ServerTimeout;

    public ZoomClient(string host, int port, ILogger logger, TimeSpan serverTimeout) : base(host, port, logger)
    {
        _comunicator = new();
        _comunicator.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        _serverEndPoint = new(IPAddress.Parse(_host), _port);
        ServerTimeout = serverTimeout;
        log.LogSuccess("Listener initialized!");
    }






    public void Send_CreateMeeting()
    {
        SendingBuffer.Add(OpCode.CREATE_MEETING.AsArray());
    }
    public void Send_CreateUser(string username)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.CREATE_USER);
            bw.Write(username);
            SendingBuffer.Add(ms.ToArray());
        }
    }
    public void Send_ChangeName(int userId, string newUSername)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.CHANGE_USER_NAME);
            bw.Write(userId);
            bw.Write(newUSername);
            SendingBuffer.Add(ms.ToArray());
        }
    }
    public void SendJoinMeetingUsingCode(int meetingCode)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING);
            bw.Write(meetingCode);
            //log.LogSuccess($"Sending request for meeting joining. Meetingcode: {meetingCode}");
            SendingBuffer.Add(ms.ToArray());
        }
    }
    public void Send_UserJoinedMeeting(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_JOINED_MEETING, userId, meetingId);
    }
    public void Send_UserLeftMeeting(int userId, int meetingId)
    {
        SendPacket(OpCode.PARTICIPANT_LEFT_MEETING, userId, meetingId);
    }
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

                    SendingBuffer.Add(ms.ToArray());
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
                    SendingBuffer.Add(ms.ToArray());
                }
            }
        }
    }
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
        var clusters = bytes.AsClusters(32768);

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_SCREEN_CAPTURE_CREATE_FRAME);
            bw.Write(userId);
            bw.Write(meetingId);
            bw.Write(clusters.Count);
            SendingBuffer.Add(ms.ToArray());

            for (int i = 0; i < clusters.Count; i++)
            {
                ms.Clear();
                bw.Write((byte)OpCode.PARTICIPANT_SCREEN_CAPTURE_UPDATE_FRAME);
                bw.Write(userId);
                bw.Write(meetingId);
                bw.Write(i);
                bw.Write(clusters[i].Length);
                bw.Write(clusters[i]);
                SendingBuffer.Add(ms.ToArray());
            }
        }
    }
    public void Send_MessageEveryone(int fromUserId, int meetingId, string message)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_MESSAGE_SEND_EVERYONE);
            bw.Write(fromUserId);
            bw.Write(meetingId);
            bw.Write(message);
            SendingBuffer.Add(ms.ToArray());
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
            SendingBuffer.Add(ms.ToArray());
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
            SendingBuffer.Add(ms.ToArray());
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
            SendingBuffer.Add(ms.ToArray());
        }
    }





    public bool SendFileEveryone(int meetingId, int senderId, string path, string fileId, CancellationToken token)
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
                    SendingBuffer.Add(ms.ToArray());
                }
            }
            if (token.IsCancellationRequested)
            {
                SendFileDelete(meetingId, fileId);
                return true;
            }
            return true;
        }
    }
    public bool SendFile(int meetingId, int senderId, int receiverId,  string path, string fileId, CancellationToken token)
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
                    SendingBuffer.Add(ms.ToArray());
                }
            }
            if (token.IsCancellationRequested)
            {
                SendFileDelete(meetingId, fileId);
                return true;
            }
            return true;
        }
    }
    public void SendFileDelete(int meetingId, string fileId)
    {
        using (var ms = new MemoryStream(16))
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_DELETE);
            bw.Write(meetingId);
            bw.Write(fileId);
            SendingBuffer.Add(ms.ToArray());
        }
    }
    public void DownloadFile(int meetingId,  string fileId, long byteIndex)
    {
        using (var ms = new MemoryStream(16))
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write((byte)OpCode.PARTICIPANT_SEND_FILE_DOWNLOAD);
            bw.Write(meetingId);
            bw.Write(fileId);
            bw.Write(byteIndex);
            SendingBuffer.Add(ms.ToArray());
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
                var packet = SendingBuffer.Take(token);
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





    private DateTime _serverLastPong { get; set; } 
    private async Task PingRemote(CancellationToken token)
    {
        try
        {
            byte[] data;

            using (var ms = new MemoryStream(8))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((byte)OpCode.PING);
                data = ms.ToArray();
            }

            _serverLastPong = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                if ((DateTime.UtcNow - _serverLastPong) > ServerTimeout)
                {
                    System.Windows.MessageBox.Show("Server is not responding", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
                else
                {
                    SendingBuffer.Add(data);
                }

                await Task.Delay(4000, token);
            }
        }
        catch(Exception ex)
        {
            log.LogError(ex.Message);
        }
    }









    protected override async Task Process(CancellationToken token)
    {
        Task.Run(() => SendingProcess(token));
        Task.Run(() => PingRemote(token));

        try
        {
            log.LogSuccess("Listener started!");

            while (true)
            {
                try
                {
                    var packet = await _comunicator.ReceiveAsync(token);
                    using var ms = new MemoryStream(packet.Buffer);
                    using var br = new PacketReader(ms);
                    var opCode = br.ReadOpCode();
                    log.LogWarning($"Received op code: {opCode}");


                    //===================================================================
                    //----PING-PONG
                    //===================================================================
                    if (opCode == OpCode.PING)
                    {
                        log.LogSuccess($"ping to server!");
                        SendingBuffer.Add(OpCode.PONG.AsArray());
                    }
                    else if (opCode == OpCode.PONG)
                    {
                        _serverLastPong = DateTime.UtcNow;
                    }
                    //===================================================================
                    //----RESULTS
                    //===================================================================
                    else if (opCode == OpCode.ERROR)
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
                        OnUser_SentAudioFrame?.Invoke(new(userId, data));
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

                        var framePosition = br.ReadInt32();
                        var dataLength = br.ReadInt32();
                        var data = br.ReadBytes(dataLength);

                        //log.LogWarning($"Received command to create camera frame! user:{userId}  clusters:{framesCount}");
                        if(framesCount == 1)
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
                            OnCameraFrameReceived?.Invoke(new (frameInfo.UserId, image));
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
                            OnScreenCaptureFrameReceived?.Invoke(new(frameInfo.UserId, image));
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
                    //----FILES
                    //===================================================================
                    else if (opCode == OpCode.PARTICIPANT_SEND_FILE_UPLOADED)
                    {
                        log.LogSuccess("RECEIVED FILE!");
                        var senderName = br.ReadString();
                        var fileName = br.ReadString();
                        var fileSize = br.ReadInt64();
                        var fileId = br.ReadString();
                        var fileModel = new FileModel(fileId, fileName, fileSize);
                        OnFileUploaded?.Invoke(new(senderName, "You", fileModel));
                    }
                    else if (opCode == OpCode.PARTICIPANT_SEND_FILE_UPLOADED_EVERYONE)
                    {
                        log.LogSuccess("RECEIVED FILE FOR EVERYONE!");
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
                        var userId = br.ReadInt32();
                        var username = br.ReadString();
                        OnUserChangedName?.Invoke(new(userId, username));
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
