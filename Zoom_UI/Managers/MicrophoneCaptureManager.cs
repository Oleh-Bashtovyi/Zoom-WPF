using NAudio.Wave;
namespace Zoom_UI.Managersl;

public class MicrophoneCaptureManager
{
    private int _maxSoundPacketSize;
    private List<byte> _soundPacket = new();
    private WaveInEvent? _currentWaveIn;
    private WaveFormat _waveFormat;
    public event Action? OnCaptureStarted;
    public event Action? OnCaptureFinished;
    public event Action<byte[]>? OnSoundCaptured;
    public bool IsMicrophonTurnedOn => _currentWaveIn != null;
    public WaveFormat GetWaveFormat => _waveFormat;

    //24576
    //16384
    public MicrophoneCaptureManager(WaveFormat waveFormat, int maxSoundPacketSize = 16384)
    {
        _waveFormat = waveFormat;
        _maxSoundPacketSize = maxSoundPacketSize;
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
        if(_currentWaveIn == null)
        {
            _currentWaveIn = new();
            _currentWaveIn.DeviceNumber = deviceNumber;
            _currentWaveIn.WaveFormat = _waveFormat; 
            _currentWaveIn.DataAvailable += WaveIn_DataAvailable;
            _currentWaveIn.StartRecording();
            _soundPacket.Clear();
            OnCaptureStarted?.Invoke();
        }
    }

    public void StopRecording()
    {
        if(_currentWaveIn != null)
        {
            _currentWaveIn.StopRecording();
            _currentWaveIn.Dispose();
            _currentWaveIn = null;
            OnCaptureFinished?.Invoke();
        }
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs args)
    {
        _soundPacket.AddRange(args.Buffer);

        if(_soundPacket.Count >= _maxSoundPacketSize)
        {
            OnSoundCaptured?.Invoke(_soundPacket.ToArray());
            _soundPacket.Clear();
        }
    }
}
