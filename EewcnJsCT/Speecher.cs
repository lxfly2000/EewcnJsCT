using System;
using System.Globalization;
using Cryville.Common.Compat;
using Cryville.EEW;
using Cryville.EEW.Report;
using Cryville.EEW.TTS;

namespace EewcnJsCT
{
    public class Speecher : IContextedGenerator<object, ITTSMessageGeneratorContext, TTSEntry>, IPropertiesHolder
    {
        public bool MyConfig { get; set; }

        public Speecher(bool myConfig)
        {
            MyConfig = myConfig;
        }
        public TTSEntry Generate(object e, ITTSMessageGeneratorContext? context, ref CultureInfo culture) {
            ThrowHelper.ThrowIfNull(e);
            context ??= EmptyTTSMessageGeneratorContext.Instance;

            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return new TTSEntry(culture, "Title", "这是一段测试语音。", 0);
        }
    }
}