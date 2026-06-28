using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Cryville.EEW;
using Microsoft.ClearScript;

namespace EewcnJsCT
{
    public class Worker: ISourceWorker,IPropertiesHolder
    {
        public static Worker lastInstance = null;
        public EewcnJs eewcnJs;

        public Worker(string webSocketEEWPingContent, string webSocketHistoryPingContent)
        {
            eewcnJs=new EewcnJs(webSocketEEWPingContent, webSocketHistoryPingContent);
        }
        public string? GetName([NotNull] ref CultureInfo? culture) {
            return Util.GetString("SourceName",ref culture);
        }

        public event Handler<object?>? Received;
        public event Handler<Heartbeat>? Heartbeat;
        public event Handler<Exception>? ErrorEmitted;
        private ScriptObject lastEEWObject = null;
        private ScriptObject lastHistoryObject = null;

        public void InvokeReceived(object obj)
        {
            //obj可能为ScriptObject类型，null或Undefined类
            ScriptObject scriptObj=(ScriptObject)obj;
            if (Util.GetScriptObjectDataType(ref scriptObj) == 1)
            {
                //EEW:data
                List<int>indexes=Util.FindUpdatedEEWObjectIndex(ref lastEEWObject,ref scriptObj);
                if (indexes.Count > 0)
                    Logger.GetInstance().info(eewcnJs.GetString("EEWReceiveDataColon") +
                                              Util.JSRootDataToString(ref scriptObj, "data", ref indexes));
                lastEEWObject = scriptObj;
                //注意CysTerra的顺序是先发送的在下面，后发送的在上面
                for (int i = indexes.Count-1; i >=0; i--)
                {
                    EEWEntry eewEntry = new EEWEntry()
                    {
                        depth = Util.GetObjectIndexFloat(ref scriptObj, "data", indexes[i], "depth"),
                        epicenter = Util.GetObjectIndexString(ref scriptObj, "data", indexes[i], "epicenter"),
                        eventId = Util.GetObjectIndexString(ref scriptObj, "data", indexes[i], "eventId"),
                        intensity = Util.GetObjectIndexFloat(ref scriptObj, "data", indexes[i], "intensity"),
                        latitude = Util.GetObjectIndexFloat(ref scriptObj, "data", indexes[i], "latitude"),
                        longitude = Util.GetObjectIndexFloat(ref scriptObj, "data", indexes[i], "longitude"),
                        magnitude = Util.GetObjectIndexFloat(ref scriptObj, "data", indexes[i], "magnitude"),
                        startAt = Util.GetObjectIndexLong(ref scriptObj, "data", indexes[i], "startAt"),
                        updates = Util.GetObjectIndexInt(ref scriptObj, "data", indexes[i], "updates"),
                    };
                    Received?.Invoke(this, eewEntry);
                    //调用OnReport，但是失效的不报
                    if (DateTimeOffset.UtcNow.Ticks <
                        Util.CalcLiveTimeTo(eewEntry.startAt, eewEntry.magnitude, eewEntry.depth).Ticks)
                    {
                        ScriptObject elemObj = Util.GetObjectIndex(ref scriptObj, "data", i);
                        string str = eewcnJs.ScriptObjectToString(ref elemObj);
                        eewcnJs.GetEEWOnReport(ref str);
                    }
                }
            }
            else if (Util.GetScriptObjectDataType(ref scriptObj) == 2)
            {
                //History:shuju
                List<int>indexes=Util.FindUpdatedHistoryObjectIndex(ref lastHistoryObject,ref scriptObj);
                if (indexes.Count > 0)
                    Logger.GetInstance().info(eewcnJs.GetString("HistoryReceiveDataColon") +
                                              Util.JSRootDataToString(ref scriptObj, "shuju", ref indexes));
                lastHistoryObject = scriptObj;
                //注意CysTerra的顺序是先发送的在下面，后发送的在上面
                for (int i = indexes.Count-1; i >=0; i--)
                {
                    Received?.Invoke(this,new HistoryEntry()
                    {
                        AUTO_FLAG = Util.GetObjectIndexString(ref scriptObj,"shuju",indexes[i], "AUTO_FLAG"),
                        EPI_DEPTH = Util.GetObjectIndexFloat(ref scriptObj, "shuju", indexes[i], "EPI_DEPTH"),
                        EPI_LAT = Util.GetObjectIndexString(ref scriptObj, "shuju", indexes[i], "EPI_LAT"),
                        EPI_LON = Util.GetObjectIndexString(ref scriptObj, "shuju", indexes[i], "EPI_LON"),
                        EQ_TYPE = "M",
                        id=Util.GetObjectIndexString(ref scriptObj, "shuju", indexes[i], "id"),
                        intensity = Util.GetObjectIndexFloat(ref scriptObj, "shuju", indexes[i], "intensity"),
                        LOCATION_C = Util.GetObjectIndexString(ref scriptObj, "shuju", indexes[i], "LOCATION_C"),
                        M=Util.GetObjectIndexString(ref scriptObj, "shuju", indexes[i], "M"),
                        O_TIME = Util.GetObjectIndexString(ref scriptObj, "shuju", indexes[i], "O_TIME"),
                    });
                    //调用OnReport
                    ScriptObject elemObj = Util.GetObjectIndex(ref scriptObj, "shuju", i);
                    string str=eewcnJs.ScriptObjectToString(ref elemObj);
                    eewcnJs.GetHistoryOnReport(ref str);
                }
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                lastInstance = this;
                Logger.GetInstance().info(eewcnJs.GetString("ProgramStarted"));
                if (eewcnJs.Init() != 0)
                {
                    Logger.GetInstance().error(eewcnJs.GetString("FailedToLoadJS"));
                    return;
                }

                Logger.GetInstance().info(eewcnJs.GetString("LoadedScriptColon") + eewcnJs.JavaScriptPath);
            }
            catch (Exception e)
            {
                Logger.GetInstance().error(e.ToString());
                return;
            }

            try {
                while (true) {
                    //Heartbeat表示程序正常工作的定期通知
                    Heartbeat?.Invoke(this,new Heartbeat());
                    //Received?.Invoke(this,"Sth");
                    // ...
                    // Fetch and parse new events if there is any
                    // ...
			
                    // Wait before next request
                    eewcnJs.RunPeriodic();
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(true);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                // Do nothing: Worker task cancellation requested
            }

            eewcnJs.End();
        }

        public void Dispose()
        {
            // Currently nothing to dispose.
        }
    }
}