using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cryville.EEW;
using Cryville.EEW.Features;

namespace EewcnJsCT
{
    [Export(typeof(IBuilder<IGenerator<Feature>>))]
    public class AnnotatF : IBuilder<Annotat> {
        public string? GetName([NotNull] ref CultureInfo? culture) {
            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return res.GetStringRequired("SourceName");
        }
        public Annotat Build(ref CultureInfo? culture) {
            // MyConfig has been set by the user here
            return new Annotat();
        }
    }
}