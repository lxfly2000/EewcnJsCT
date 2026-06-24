using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Cryville.EEW;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

namespace EewcnJsCT
{
    public class Worker: ISourceWorker
    {
        public bool MyConfig { get; set; }

        public Worker(bool myConfig)
        {
            MyConfig = myConfig;
        }
        public string? GetName([NotNull] ref CultureInfo? culture) {
            // Get the name of the source worker from the resources
            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return res.GetStringRequired("SourceName");
        }

        public event Handler<object?>? Received;
        public event Handler<Heartbeat>? Heartbeat;
        public event Handler<Exception>? ErrorEmitted;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            ScriptEngine jsEngine = new V8ScriptEngine();//TODO:加载JS文件
            try {
                while (true) {
                    //Heartbeat表示程序正常工作的定期通知
                    Heartbeat?.Invoke(this,new Heartbeat());
                    Received?.Invoke(this,"Pl");
                    // ...
                    // Fetch and parse new events if there is any
                    // ...
			
                    // Wait before next request
                    await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken).ConfigureAwait(true);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                // Do nothing: Worker task cancellation requested
            }
        }

        public void Dispose()
        {
            // Currently nothing to dispose.
        }
    }
}