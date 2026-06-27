using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cryville.EEW;
using Cryville.EEW.TTS;

namespace EewcnJsCT
{
    [Export(typeof(IBuilder<IGenerator<TTSEntry>>))]
    public class SpeechF : IBuilder<Speecher> {
        public string? GetName([NotNull] ref CultureInfo? culture) {
            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return res.GetStringRequired("SourceName");
        }
        public Speecher Build(ref CultureInfo? culture) {
            return new Speecher();
        }
    }
}