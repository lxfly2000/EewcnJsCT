using System.Globalization;
using System.Security;
using System.Speech.Synthesis;

namespace TestLib;

public class TestTTS
{
    public TestTTS()
    {
        string text = "你好，世界。";
        string bcp47 = "zh-CN"; // BCP47 语言标签

        using (var synth = new SpeechSynthesizer())
        {
            // 列出可用语音和语言（调试看有哪些安装的 voice/culture）
            foreach (var v in synth.GetInstalledVoices())
            {
                Console.WriteLine($"{v.VoiceInfo.Name} - {v.VoiceInfo.Culture.Name}");
            }

            // 尝试按 Culture (BCP47) 选择语音（如果机器上有匹配的 voice）
            try
            {
                synth.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new CultureInfo(bcp47));
            }
            catch
            {
                Console.WriteLine($"未找到匹配 {bcp47} 的语音，继续使用默认语音。");
            }

            // 方式 A：直接朗读文本
            synth.SetOutputToDefaultAudioDevice();
            synth.Speak(text);

            // 方式 B：使用 SSML 明确指定 xml:lang（推荐，能确保 TTS 引擎按语言解析）
            string ssml = $@"<speak version=""1.0"" xml:lang=""{bcp47}"">
                                <voice xml:lang=""{bcp47}"">{SecurityElement.Escape(text)}</voice>
                             </speak>";
            synth.SpeakSsml(ssml);
        }
    }
}