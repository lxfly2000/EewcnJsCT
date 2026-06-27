using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cryville.EEW;
using Cryville.EEW.ComponentModel;

namespace EewcnJsCT
{
    [Export(typeof(IBuilder<ISourceWorker>))]
    public class WorkerF : IBuilder<Worker>
    {
        [LocalizableDisplayName("$WSEEWPingContentName")]
        [LocalizableDescription("$WSEEWPingContentDescription")]
        public string WebSocketEewPingContent { get; set; } = "ping";

        [LocalizableDisplayName("$WSHistoryPingContentName")]
        [LocalizableDescription("$WSHistoryPingContentDescription")]
        public string WebSocketHistoryPingContent { get; set; } = "ping";
        
        public string? GetName([NotNull] ref CultureInfo? culture) {
            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return res.GetStringRequired("SourceName");
        }
        public Worker Build(ref CultureInfo? culture) {
            return new Worker(WebSocketEewPingContent,WebSocketHistoryPingContent);
        }
    }
}