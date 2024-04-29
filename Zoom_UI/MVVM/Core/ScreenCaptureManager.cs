using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WebEye.Controls.Wpf;
using Zoom_Server.Net;
using Zoom_UI.MVVM.Models;

namespace Zoom_UI.MVVM.Core;

public class ScreenCaptureManager
{
    private CancellationTokenSource ScreenTokenSource = new();
    public Bitmap? CurrentBitmap { get; private set; }
    public int Height { get; private set; }
    public int Width { get; private set; }


    public event Action<Bitmap?>? OnImageCaptured;
    public event Action<ErrorModel>? OnError;
    public event Action? OnCaptureStarted;
    public event Action? OnCaptureFinished;


    public ScreenCaptureManager(int height, int width)
    {
        Height = height;
        Width = width;
        ScreenTokenSource.Cancel();
    }




    public void StartCapturing(int fps)
    {
        if (ScreenTokenSource != null && !ScreenTokenSource.IsCancellationRequested)
        {
            return;
        }

        ScreenTokenSource?.Dispose();
        ScreenTokenSource = new();

        Application.Current.Dispatcher.Invoke(() =>
        {
            Task.Run(async () => await CaptureProcess(fps, ScreenTokenSource.Token));
        });
    }


    public void StopCapturing()
    {
        ScreenTokenSource.Cancel();
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
                    CurrentBitmap = Screenshot();
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





    private Bitmap Screenshot()
    {
        // Get the size of the primary screen using SystemParameters
        /*        int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                int screenHeight = (int)SystemParameters.PrimaryScreenHeight;*/

        Bitmap screenshot = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(screenshot))
        {
            graphics.CopyFromScreen(
                0, 0, 0, 0,
                new System.Drawing.Size(Width, Height));
        }
        return screenshot;
    }
}
