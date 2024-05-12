using NAudio.Wave;
namespace Zoom_UI.Managersl;

public class MicrophoneCaptureManager
{
    private int MaxSoundPacketSize = 32768;
    private List<byte> SoundPacket = new();
    private WaveInEvent? _currentWaveIn;
    public event Action? OnCaptureStarted;
    public event Action? OnCaptureFinished;
    public event Action<byte[]>? OnSoundCaptured;

    public WaveFormat GetWaveFormat => new(Rate, Bits, Chanels); //audio format(44.1kHz, mono)
    public bool IsMicrophonTurnedOn => _currentWaveIn != null;
    public int Rate { get; private set; }
    public int Bits {  get; private set; }
    public int Chanels {  get; private set; }

    //24576
    //16384
    public MicrophoneCaptureManager(int maxSoundPacketSize = 16384, int rate = 44100, int bits = 16, int chanels = 1)
    {
        Rate = rate;
        Bits = bits;
        Chanels = chanels;
        MaxSoundPacketSize = maxSoundPacketSize;
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
            _currentWaveIn.WaveFormat = GetWaveFormat; 
            _currentWaveIn.DataAvailable += WaveIn_DataAvailable;
            _currentWaveIn.StartRecording();
            SoundPacket.Clear();
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
/*        byte[] soundBytes = new byte[args.BytesRecorded];

        Array.Copy(args.Buffer, soundBytes, args.BytesRecorded);*/

        SoundPacket.AddRange(args.Buffer);

        if(SoundPacket.Count >= MaxSoundPacketSize)
        {
            OnSoundCaptured?.Invoke(SoundPacket.ToArray());
            SoundPacket.Clear();
        }
    }
}
