using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Zoom_UI.Managersl;

public class MicrophoneCaptureManager
{

    private WaveInEvent? _currentWaveIn;
    public event Action? OnCaptureStarted;
    public event Action? OnCaptureFinished;
    public event Action<byte[]>? OnSoundCaptured;


    public bool IsMicrophonTurnedOn => _currentWaveIn != null;
    public int Rate { get; private set; }
    public int Bits {  get; private set; }
    public int Chanels {  get; private set; }
    public WaveFormat GetWaveFormat => new WaveFormat(Rate, Bits, Chanels);

    public MicrophoneCaptureManager(int rate = 44100, int bits = 16, int chanels = 1)
    {
        Rate = rate;
        Bits = bits;
        Chanels = chanels;
    }


    public IEnumerable<WaveInCapabilities> GetInputDevices()
    {
        var list = new List<WaveInCapabilities>();

        for (int i = 0; i < WaveIn.DeviceCount; i++)
        { 
            list.Add(WaveIn.GetCapabilities(i));
        }

        return list;
    }


    public void StartRecording(int deviceNumber)
    {
        _currentWaveIn = new();
        _currentWaveIn.DeviceNumber = deviceNumber;
        _currentWaveIn.WaveFormat = GetWaveFormat; // Set the desired audio format (44.1kHz, mono)
        _currentWaveIn.DataAvailable += WaveIn_DataAvailable; // Assign the event handler for new audio data
        _currentWaveIn.StartRecording();
        OnCaptureStarted?.Invoke();
    }


    public void StopRecording()
    {
        if(_currentWaveIn != null)
        {
            _currentWaveIn?.StopRecording();
            _currentWaveIn?.Dispose();
            _currentWaveIn=null;
            OnCaptureFinished?.Invoke();
        }
    }


    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs args)
    {
        byte[] soundBytes = new byte[args.BytesRecorded];

        Array.Copy(args.Buffer, soundBytes, args.BytesRecorded);

        OnSoundCaptured?.Invoke(soundBytes);
    }
}
