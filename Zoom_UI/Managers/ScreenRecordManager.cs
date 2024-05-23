using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Xabe.FFmpeg;
using Zoom_Server.Net.Codes;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.Managers
{
    public class ScreenRecordingManager : IDisposable
    {
        private readonly string args = $"-f gdigrab -framerate 30 -draw_mouse 0 -i title=\"MyZoom\" -c:v libx264 -preset ultrafast -tune zerolatency";
        private readonly string _ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
        private readonly string _outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Records");
        private readonly  string _tempName;
        public event Action? OnRecordStarted;
        public event Action? OnRecordFinished;
        public event Action<ErrorModel>? OnError;
        private  Process _ffmpegProcess;
        public bool IsRecordingNow { get; private set; }


        public ScreenRecordingManager()
        {
            _tempName = $"{DateTime.Now.Ticks}.mp4";
            _ffmpegProcess = new Process();
            _ffmpegProcess.StartInfo.FileName = _ffmpegPath;
            _ffmpegProcess.StartInfo.Arguments = $"{argsS} {_outputPath}\\{_tempName}";
            _ffmpegProcess.StartInfo.UseShellExecute = false;
            _ffmpegProcess.StartInfo.RedirectStandardInput = true;
            _ffmpegProcess.StartInfo.CreateNoWindow = true;
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Records" ));
        }


        public void StartRecording()
        {
            try
            {
                OnRecordStarted?.Invoke();
                _ffmpegProcess.Start();
                IsRecordingNow = true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new(ErrorCode.GENERAL, ex.Message));
                OnRecordFinished?.Invoke();
            }
        }

        public void StopRecording()
        {
            try
            {
                using (var writer = _ffmpegProcess.StandardInput)
                {
                    if (writer.BaseStream.CanWrite)
                    {
                        writer.WriteLine("q");
                    }
                }

                _ffmpegProcess.WaitForExit();

                var source = $"{_outputPath}\\{_tempName}";

                if (File.Exists(source))
                {
                    var fi = new FileInfo(source);
                    var newName = $"Record_{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.mp4";
                    var dest = $"{_outputPath}\\{newName}";
                    fi.MoveTo(dest);
                }

                IsRecordingNow = false;
                OnRecordFinished?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new(ErrorCode.GENERAL, ex.Message));
            }
        }

        public void Dispose()
        {
            if( _ffmpegProcess != null )
            {
                _ffmpegProcess.Close();
                _ffmpegProcess.Dispose();
            }
        }
    }
}

















/*using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Xabe.FFmpeg;
using Zoom_Server.Net.Codes;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.Managers
{
    public class ScreenRecordingManager
    {
        //private const string _ffmpegPath = @"E:\ffmpeg\bin\ffmpeg.exe";
        private const string args =
            "-f gdigrab " +
            "-framerate 30 " +
            "-draw_mouse 0 " +
            "-i desktop " +
            "-c:v libx264 " +
            "-preset ultrafast " +
            "-tune zerolatency " +
            "-pix_fmt yuv420p " +
            "-c:a aac";

        //private readonly string _ffmpegPath = @"E:\ffmpeg\bin\ffmpeg.exe";
        private readonly string _ffmpegPath = @"E:\ffmpeg\ffmpeg-2024-05-20-git-127ded5078-full_build\bin\ffmpeg.exe";
        private readonly string _outputPath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _tempName;
        private Process _ffmpegProcess;

        public event Action? OnRecordStarted;
        public event Action? OnRecordFinished;
        public event Action<ErrorModel>? OnError;
        public bool IsRecordingNow { get; private set; }


        public ScreenRecordingManager()
        {
            _tempName = $"{DateTime.Now.Ticks}.mp4";

            _ffmpegProcess = new Process();
            _ffmpegProcess.StartInfo.FileName = _ffmpegPath;
            _ffmpegProcess.StartInfo.Arguments = $"{GetArgs()} {Application.ExecutablePath}\\{_tempName}";
            _ffmpegProcess.StartInfo.UseShellExecute = false;
            _ffmpegProcess.StartInfo.RedirectStandardInput = true;
            _ffmpegProcess.StartInfo.CreateNoWindow = true;
        }


        private string GetArgs()
        {
            *//*            string processName = Process.GetCurrentProcess().ProcessName;
                        //string processName = "MainWindow";

                        string args =
                                "-f gdigrab " +
                                "-framerate 30 " +
                                "-draw_mouse 0 " +
                                $"-i \"{processName}\" " +
                                "-c:v libx264 " +
                                "-preset ultrafast " +
                                "-tune zerolatency " +
                                "-pix_fmt yuv420p " +
                                "-c:a aac";

                        return args;*//*

            return args;
        }



        public void StartRecording()
        {
            try
            {
                _ffmpegProcess.Start();
                IsRecordingNow = true;
                OnRecordStarted?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new(ErrorCode.GENERAL, ex.Message));
            }
        }

        public void StopRecording()
        {
            try
            {
                using (var writer = _ffmpegProcess.StandardInput)
                {
                    if (writer.BaseStream.CanWrite)
                    {
                        writer.WriteLine("q");
                    }
                }

                _ffmpegProcess.WaitForExit();

                var source = $"{_outputPath}\\{_tempName}";

                if (File.Exists(source))
                {
                    var fileInfo = new FileInfo(source);
                    var newName = $"Record_{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.mp4";
                    var dest = $"{_outputPath}\\{newName}";

                    fileInfo.MoveTo(dest);
                }

                IsRecordingNow = false;
                OnRecordFinished?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new(ErrorCode.GENERAL, ex.Message));
            }
        }
    }
}*/



