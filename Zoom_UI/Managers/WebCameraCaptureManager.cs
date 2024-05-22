using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using WebEye.Controls.Wpf;
using Zoom_Server.Net.Codes;
using Zoom_UI.MVVM.Models;
namespace Zoom_UI.Managers;


public class WebCameraCaptureManager 
{
    private DispatcherTimer _timer = new();
    public WebCameraControl WebCamera { get; private set; }
    public Bitmap? CurrentBitmap { get; private set; }
    public int Fps {  get; private set; }
    public TimeSpan GetIntervalBetweenFrames => TimeSpan.FromMilliseconds(1000.0 / Fps);

    public event Action<Bitmap?>? OnImageCaptured;
    public event Action<ErrorModel>? OnError;
    public event Action? OnCaptureStarted;
    public event Action? OnCaptureFinished;

    public WebCameraCaptureManager(WebCameraControl webCamera, int fps = 15)
    {
        WebCamera = webCamera;
        Fps = fps;
        _timer.Interval = GetIntervalBetweenFrames;
        _timer.Tick += Timer_Tick;
    }

    public IEnumerable<WebCameraId> GetInputDevices()
    {
        return WebCamera.GetVideoCaptureDevices();
    }

    public void StartCapturing(WebCameraId cameraId)
    {
        try
        {
            if (!_timer.IsEnabled)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WebCamera.StartCapture(cameraId);
                });

                _timer.Start();
                OnCaptureStarted?.Invoke();
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new(ErrorCode.GENERAL, "(CAPTURE START ERROR) - " +  ex.Message));
        }
    }

    public void StopCapturing()
    {
        try
        {
            if (_timer.IsEnabled)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WebCamera.StopCapture();
                });

                _timer.Stop();
                OnCaptureFinished?.Invoke();
            }
        }
        catch(Exception ex) 
        {
            OnError?.Invoke(new(ErrorCode.GENERAL, "(CAPTURE STOP ERROR) - " + ex.Message));  
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (WebCamera.IsCapturing)
                {
                    CurrentBitmap = WebCamera.GetCurrentImage();

                }
            });

            OnImageCaptured?.Invoke(CurrentBitmap);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new(ErrorCode.GENERAL, "(CAPTURE PROCESS ERROR) - " + ex.Message));
            StopCapturing();
        }
    }
}
