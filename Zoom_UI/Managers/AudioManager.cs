using NAudio.Wave;

namespace Zoom_UI.Managers
{
    internal class AudioManager : IDisposable
    {
        private WaveOut _waveOut = new();
        private BufferedWaveProvider waveProvider;


        internal AudioManager(WaveFormat waveFormat)
        {
            waveProvider = new BufferedWaveProvider(waveFormat);
            _waveOut.Init(waveProvider);
        }

        internal void Play()
        {
            _waveOut.Play();
        }

        internal void Stop()
        {
            _waveOut.Stop();
        }

        internal void PlayAudio(byte[] audio)
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
