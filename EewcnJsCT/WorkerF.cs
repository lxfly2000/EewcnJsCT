using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cryville.EEW;

namespace EewcnJsCT
{
    [Export(typeof(IBuilder<ISourceWorker>))]
    public class WorkerF : IBuilder<Worker> {
        public bool MyConfig { get; set; } = false;
        
        public string? GetName([NotNull] ref CultureInfo? culture) {
            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return res.GetStringRequired("SourceName");
        }
        public Worker Build(ref CultureInfo? culture) {
            return new Worker(MyConfig);
        }
    }
}