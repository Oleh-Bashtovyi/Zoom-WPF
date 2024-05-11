using System.Drawing;
using System.Windows;
using System.Windows.Media.Media3D;
using WebEye.Controls.Wpf;
using Zoom_Server.Net.Codes;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.Managers;


public interface IImageCaptureService
{
    public event Action<Bitmap>? OnImageCaptured;
    public event Action? OnCaptureStarted;
    public event Action? OnCaptureFinished;
    public event Action<ErrorModel>? OnError;

    public Bitmap? CurrentBitmap { get; }

    public void StartCapturing();
    public void StopCapturing();
}




public class WebCameraCaptureManager //: IImageCaptureService
{
    private CancellationTokenSource CameraTokenSource = new();
    public WebCameraControl WebCamera { get; private set; }
    public Bitmap? CurrentBitmap { get; private set; }


    public event Action<Bitmap?>? OnImageCaptured;
    public event Action<ErrorModel>? OnError;
    public event Action? OnCaptureStarted;
    public event Action? OnCaptureFinished;


    public WebCameraCaptureManager(WebCameraControl webCamera)
    {
        WebCamera = webCamera;
        CameraTokenSource.Cancel();
    }

    public IEnumerable<WebCameraId> GetInputDevices()
    {
        return WebCamera.GetVideoCaptureDevices();
    }


    public void StartCapturing(WebCameraId cameraId, int fps)
    {
        if (CameraTokenSource != null && !CameraTokenSource.IsCancellationRequested)
        {
            return;
        }

        CameraTokenSource?.Dispose();
        CameraTokenSource = new();

        Application.Current.Dispatcher.Invoke(() =>
        {
            WebCamera.StartCapture(cameraId);
            Task.Run(async () => await CaptureProcess(fps, CameraTokenSource.Token));
        });
    }


    public void StopCapturing()
    {
        try
        {
            CameraTokenSource.Cancel();

            Application.Current.Dispatcher.Invoke(() =>
            {
                WebCamera.StopCapture();
            });
        }
        catch (Exception)
        {
        }
    }



    private async Task CaptureProcess(int fps, CancellationToken token)
    {
        try
        {
            OnCaptureStarted?.Invoke();

            var delay = (int)(1000d / fps);

            while (true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (WebCamera.IsCapturing)
                    {
                        CurrentBitmap = WebCamera.GetCurrentImage();

                    }
                });

                OnImageCaptured?.Invoke(CurrentBitmap);

                await Task.Delay(delay, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            OnError?.Invoke(new(ErrorCode.GENERAL, ex.Message));
        }
        finally
        {
            OnCaptureFinished?.Invoke();
        }
    }
}
