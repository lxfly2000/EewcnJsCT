using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cryville.EEW;
using Cryville.EEW.Report;

namespace EewcnJsCT
{
    [Export(typeof(IBuilder<IGenerator<ReportModel>>))]
    public class ReporterF : IBuilder<Reporter> {
        public string? GetName([NotNull] ref CultureInfo? culture) {
            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return res.GetStringRequired("SourceName");
        }
        public Reporter Build(ref CultureInfo? culture) {
            // MyConfig has been set by the user here
            return new Reporter();
        }
    }
}