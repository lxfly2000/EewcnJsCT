using System;
using System.Globalization;
using System.Speech.Synthesis;

namespace EewcnJsCT
{
    public class JSTTS
    {
        private static JSTTS singleton = null;

        public static JSTTS GetInstance()
        {
            if (singleton == null)
                singleton = new JSTTS();
            return singleton;
        }

        private SpeechSynthesizer synth = null;

        private JSTTS()
        {
            try
            {
                synth = new SpeechSynthesizer();
            }
            catch (PlatformNotSupportedException e)
            {
                Logger.GetInstance().warn(Worker.lastInstance.eewcnJs.GetString("SpeechPlatformNotSupportedExceptionColon")+e.Message);
            }
        }
        
        public void play(string langTag, string msg)
        {
            if (synth == null)
                return;
            if (synth.State == SynthesizerState.Speaking)
                synth.Pause();
            try
            {
                synth.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new CultureInfo(langTag));
            }
            catch
            {
                Logger.GetInstance()
                    .warn(Worker.lastInstance.eewcnJs.GetString("SpeechInLangTagNotFoundColon") + langTag);
            }

            synth.SetOutputToDefaultAudioDevice();
            synth.Speak(msg);
        }
    }
}