using System.Drawing;
using System.Windows;
using Zoom_UI.MVVM.Models;
using System.Drawing.Imaging;
using System.Windows.Threading;
using System.Windows.Media.Media3D;
using Zoom_Server.Net.Codes;
namespace Zoom_UI.Managers;

public class ScreenCaptureManager
{
    private DispatcherTimer Timer = new ();
    public Bitmap? CurrentBitmap { get; private set; }
    public int CaptureHeight { get; private set; }
    public int CaptureWidth { get; private set; }
    public int Fps { get; private set; }
    public TimeSpan GetIntervalBetweenFrames => TimeSpan.FromMilliseconds(1000.0 / Fps);


    public event Action<Bitmap?>? OnImageCaptured;
    public event Action<ErrorModel>? OnError;
    public event Action? OnCaptureStarted;
    public event Action? OnCaptureFinished;


    public ScreenCaptureManager(int captureHeight, int captureWidth, int fps = 15)
    {
        Fps = fps;
        CaptureHeight = captureHeight;
        CaptureWidth = captureWidth;
        Timer.Interval = GetIntervalBetweenFrames;
        Timer.Tick += Timer_Tick;
    }


    public void StartCapturing()
    {
        if (!Timer.IsEnabled)
        {
            Timer.Start();
            OnCaptureStarted?.Invoke();
        }
    }

    public void StopCapturing()
    {
        if(Timer.IsEnabled)
        {
            Timer.Stop();
            OnCaptureFinished?.Invoke();
        }
    }


    private void Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentBitmap = Screenshot();
            });

            OnImageCaptured?.Invoke(CurrentBitmap);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new(ErrorCode.GENERAL, ex.Message));
            throw;
        }
    }



    private Bitmap Screenshot()
    {
        var screenshot = new Bitmap(CaptureWidth, CaptureHeight, PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(screenshot))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(CaptureWidth, CaptureHeight));
        }
        return screenshot;
    }
}
