using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace EewcnJsCT
{
    public class JSSound
    {
        private static JSSound singleton = null;

        public static JSSound GetInstance()
        {
            if (singleton == null)
                singleton = new JSSound();
            return singleton;
        }

        private WaveOutEvent waveOutDevice = new WaveOutEvent();
        public void play(string uri, int repeats)
        {
            if (uri.ToLower().StartsWith("file:///"))
            {
                uri = uri.Substring(7);
                if (!File.Exists(uri))
                    uri = uri.Substring(1);
            }
            PlayRepeats(uri, repeats);
        }

        private async Task PlayRepeats(string uri, int repeats)
        {
            AudioFileReader audioFile = new AudioFileReader(uri);
            if(waveOutDevice.PlaybackState == PlaybackState.Playing)
                waveOutDevice.Stop();
            waveOutDevice.Init(audioFile);
            for (int i = 0; i < repeats; i++)
            {
                audioFile.Position = 0;
                waveOutDevice.Play(); //异步函数
                while (waveOutDevice.PlaybackState == PlaybackState.Playing)
                    await Task.Delay(1000);
            }
        }
    }
}