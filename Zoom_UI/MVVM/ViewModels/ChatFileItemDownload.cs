using System.IO;
using System.Windows;
using System.Windows.Input;
using Zoom_UI.ClientServer;
using Zoom_UI.MVVM.Core;
using Zoom_UI.MVVM.Models.Frames;
using Microsoft.Win32;
namespace Zoom_UI.MVVM.ViewModels;

public class ChatFileItemDownload : ViewModelBase
{
    private string _buttonLabel;
    private bool _isDownloading = false;
    private long _byteIndex = 0;
    private int _meetingId;
    private ZoomClient _client;
    private const int _bufferSize = 32768;

    public string FileName { get; private set; }
    public string FileId { get; private set; }
    public long FileLength { get; private set; }
    public string OutputPath { get; private set; }

    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetAndNotifyPropertyChanged(ref _isDownloading, value);
    }
    public string ButtonLabel
    {
        get => _buttonLabel;
        set => SetAndNotifyPropertyChanged(ref _buttonLabel, value);
    }
    public ICommand DownloadFileCommand { get; private set; }



    public ChatFileItemDownload(long fileLength, string fileName, string idName, int meetingId, ZoomClient client)
    {
        FileLength = fileLength;
        FileName = fileName;
        FileId = idName;
        _meetingId = meetingId;
        _client = client;
        ButtonLabel = "Download";
        DownloadFileCommand = new RelayCommand(DownloadFile);
        _client.OnFilePartDownloaded += OnFilePartDownloaded;
    }


    private void DownloadFile()
    {
        if (!IsDownloading)
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = FileName;
            var ext = Path.GetExtension(FileName);
            sfd.Filter = $"File (*{ext})|*{ext}";

            var path = "";

            if (sfd.ShowDialog() == true)
            {
                path = sfd.FileName;
            }

            if (path != "")
            {
                OutputPath = path;
                IsDownloading = true;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ButtonLabel = "Cancel download...";
                });

                _byteIndex = 0;

                Download();
            }
        }
        else
        {
            IsDownloading = false;

            if (File.Exists(OutputPath))
            {
                File.Delete(OutputPath);
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                ButtonLabel = "Download";
            });
        }
    }


    private void Download()
    {
        _client.DownloadFile(_meetingId, FileId, _byteIndex);
    }


    private void OnFilePartDownloaded(FileFrame frame)
    {
        if (frame.FileId == FileId && IsDownloading)
        {
            using (var file = File.OpenWrite(OutputPath))
            {
                file.Seek(_byteIndex, SeekOrigin.Begin);
                file.Write(frame.Data);
            }
            _byteIndex += _bufferSize;

            if (_byteIndex >= FileLength)
            {
                IsDownloading = false;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ButtonLabel = "Download";
                });
            }
            else
            {
                Download();
            }
        }
    }
}
