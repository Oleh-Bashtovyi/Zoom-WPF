using NAudio.Wave;

namespace Zoom_UI.Managers
{
    public class AudioManager : IDisposable
    {
        private WaveOut _waveOut = new();
        private BufferedWaveProvider waveProvider;


        public AudioManager(WaveFormat waveFormat)
        {
            waveProvider = new BufferedWaveProvider(waveFormat);
            _waveOut.Init(waveProvider);
        }

        public void Play()
        {
            waveProvider.ClearBuffer();
            _waveOut.Play();
        }

        public void Stop()
        {
            _waveOut.Stop();
        }

        public void PlayAudio(byte[] audio)
        {
            waveProvider.AddSamples(audio, 0, audio.Length);
        }

        public void Dispose()
        {
            _waveOut.Stop();
            _waveOut.Dispose();
        }
    }
}
