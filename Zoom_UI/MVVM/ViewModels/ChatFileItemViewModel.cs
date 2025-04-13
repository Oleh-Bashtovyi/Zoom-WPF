using System.IO;
using System.Windows;
using System.Windows.Input;
using Zoom_UI.ClientServer;
using Zoom_UI.MVVM.Core;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.MVVM.ViewModels;


public class ChatFileItem
{
    public event Action<ChatFileItem>? Deleted;
    public event Action<ChatFileItem>? Uploaded;

    private CancellationTokenSource _cts = new CancellationTokenSource();

    private UserViewModel _receiver;
    private UserViewModel _sender;
    private ZoomClient _zoomClient;
    public MessageModel Wraper { get; private set; }
    public int MeetingId {  get; private set; }
    public long FileSize { get; private set; }
    public string FilePath { get; private set; } = "";
    public string FileName { get; private set; }
    public string FileId { get; private set; }

    public ICommand CancelSendingCommand { get; private set; }

    public ChatFileItem(
        FileInfo file, 
        UserViewModel sender, 
        UserViewModel? receiver, 
        MessageModel wraper, 
        ZoomClient zoomClient,
        MessageModel parent, 
        int meetingId)
    {
        _receiver = receiver;
        _sender = sender;
        _zoomClient = zoomClient;
        Wraper = parent;
        MeetingId = meetingId;
        FileSize = file.Length;
        FileName = file.Name;
        FilePath = file.FullName;
        FileId = Guid.NewGuid().ToString();
        CancelSendingCommand = new RelayCommand(CancelSending);
    }

    public async void OnLoaded()
    {
        var token = _cts.Token;

        await Task.Run(() =>
        {
            bool res;
            if (_receiver == null)
            {
                res = _zoomClient.Send_FileEveryone(MeetingId, _sender.Id, FilePath, FileId, token);
            }
            else
            {
                res = _zoomClient.Send_File(MeetingId, _sender.Id, _receiver.Id, FilePath, FileId, token);
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                if (res)
                    Uploaded?.Invoke(this);
                else
                {
                    Deleted?.Invoke(this);
                    Uploaded = null;
                    Deleted = null;
                }
            });
        }, token);
    }

    private void CancelSending()
    {
        _cts.Cancel();
    }
}
