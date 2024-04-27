using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Zoom_Server.Extensions;
using Zoom_Server.Logging;
using Zoom_Server.Net;
using Zoom_UI.Extensions;
using Zoom_UI.MVVM.Models;
using static Zoom_Server.Net.PacketReader;

namespace Zoom_UI.ClientServer;

public class UdpComunicator : OneProcessServer
{
    private UdpClient _comunicator;
    private IPEndPoint _serverEndPoint;
    private Dictionary<int, FrameBuilder> User_CameraFrame = new();

    public event Action<UserModel>? OnUserCreated;
    public event Action<UserModel>? OnUserChangedName;
    public event Action<UserModel>? OnUserReceivedId;
    public event Action<ErrorModel>? OnError;

    public event Action<MeetingInfo>? OnMeetingCreated;
    public event Action<MeetingInfo>? OnUserJoinedMeeting_UsingCode;

    public event Action<UserModel>? OnUserJoinedMeeting;
    public event Action<UserModel>? OnUserLeftMeeting;

    public event Action<CameraFrame>? OnCameraFrameUpdated;
    public event Action<MessageInfo>? OnMessageSent;



    public UdpComunicator(string host, int port, ILogger logger) : base(host, port, logger)
    {
        _comunicator = new();
        _comunicator.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        _serverEndPoint = new(IPAddress.Parse(_host), _port);
        log.LogSuccess("Listener initialized!");
    }


    public async Task SEND_CREATE_USER(string username)
    {
        log.LogSuccess($"Sending request for user creation! Username: {username}");
        using var pb = new PacketBuilder();
        pb.Write(OpCode.CREATE_USER);
        pb.Write(username);
        log.LogSuccess($"Sending request for user creation! Username: {username}");
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }

    public async Task SEND_CHANGE_NAME(int userId, string newUSername)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.CHANGE_USER_NAME);
        pb.Write_UserInfo(userId, newUSername);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }


    public async Task SEND_CREATE_MEETING()
    {
        await _comunicator.SendAsync(OpCode.CREATE_MEETING.AsArray(), _serverEndPoint);
    }

    public async Task SEND_CAMERA_FRAME(int fromUser_id, Bitmap bitmap)
    {
        var bytes = bitmap.AsByteArray();
        var clusters = bytes.AsClusters(8192);
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


    public async Task Send_JoinUsingMeetingUsingCode(int meetingCode)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_USES_CODE_TO_JOIN_MEETING);
        pb.Write(meetingCode);
        log.LogSuccess($"Sending request for meeting joining. Meetingcode: {meetingCode}");
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }


    public async Task Send_JoinedMeeting(int userId, int meetingCode)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_JOINED_MEETING);
        pb.Write(userId);
        pb.Write(meetingCode);
        log.LogSuccess($"Sending request that we have joined meeting. Meetingcode: {meetingCode}");
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
    }


    public async Task SEND_LEAVE_MEETING(int userId, string username)
    {
        using var pb = new PacketBuilder();
        pb.Write(OpCode.PARTICIPANT_LEFT_MEETING);
        pb.Write_UserInfo(userId, username);
        await _comunicator.SendAsync(pb.ToArray(), _serverEndPoint);
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

                    if (opCode == OpCode.CREATE_MEETING)
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
                        OnUserLeftMeeting?.Invoke(new(userInfo.Id, userInfo.Username));
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
                        OnUserJoinedMeeting?.Invoke(new(userId, userName));
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
                            OnCameraFrameUpdated?.Invoke(new (frameInfo.UserId, image));
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
