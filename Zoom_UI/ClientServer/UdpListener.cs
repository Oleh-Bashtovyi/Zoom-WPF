using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Zoom_Server.Logging;
using Zoom_Server.Net;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.ClientServer;

internal class UdpListener : OneProcessServer
{
    private UdpClient _listener;

    public event Action<UserModel>? OnUserJoinedMeeting;
    public event Action<UserModel>? onUserLeftMeeting;
    public event Action<UserModel>? OnUserCreated;
    public event Action<MeetingInfo>? OnMeetingCreated;
    public event Action<CameraFrame>? OnCameraFrameUpdated;
    public event Action<MessageInfo>? OnMessageSent;



    public UdpListener(string host, int port, ILogger logger) : base(host, port, logger)
    {
        _listener = new UdpClient();
        _listener.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        log.LogSuccess("Listener initialized!");
    }


    public async Task SomeOper()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        var serverEndPoint = new IPEndPoint(IPAddress.Parse(_host), _port);
        bw.Write((byte)OpCode.CreateUser);
        bw.Write("This is Username!");
        await _listener.SendAsync(ms.ToArray(), (int)ms.Length, serverEndPoint);
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
                    await Task.Delay(2000);

                    //OnUserCreated(new(12345, "HELLLOOO"));

                    log.LogSuccess("Waiting for packets...");
                    var packet = await _listener.ReceiveAsync(token);
                    var ms = new MemoryStream(packet.Buffer);
                    var br = new BinaryReader(ms);
                    var opCode = (OpCode)br.ReadByte();
                    log.LogWarning($"Received op code: {opCode}");

                    if (opCode == OpCode.CreateMeeting)
                    {
                        var id = br.ReadInt32();
                        OnMeetingCreated?.Invoke(new() { Id = id });
                    }
                    else if (opCode == OpCode.CreateUser)
                    {
                        var id = br.ReadInt32();
                        var username = br.ReadString();
                        log.LogWarning($"Received new user!");
                        OnUserCreated?.Invoke(new(id, username));
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
