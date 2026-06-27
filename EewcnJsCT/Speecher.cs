using System.Globalization;
using Cryville.Common.Compat;
using Cryville.EEW;
using Cryville.EEW.TTS;

namespace EewcnJsCT
{
    public class Speecher : IContextedGenerator<object, ITTSMessageGeneratorContext, TTSEntry>, IPropertiesHolder
    {
        public Speecher()
        {
        }
        public TTSEntry Generate(object e, ITTSMessageGeneratorContext? context, ref CultureInfo culture) {
            ThrowHelper.ThrowIfNull(e);
            //TODO：处理TTS数据
            context ??= EmptyTTSMessageGeneratorContext.Instance;

            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return new TTSEntry(culture, "", "", 0);
        }
    }
}