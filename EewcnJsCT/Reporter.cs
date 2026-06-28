using Cryville.Common.Compat;
using Cryville.EEW;
using Cryville.EEW.Report;
using System;
using System.Globalization;
using Microsoft.ClearScript;

namespace EewcnJsCT {
    public class Reporter : IContextedGenerator<object, IReportGeneratorContext, ReportModel>, IPropertiesHolder
    {
        public Reporter()
        {
        }

        public class UpdatesKey : IReportRevisionKey
        {
            private int eewUpdates = 1;
            public bool IsCancellation => false;
            public bool IsFinalRevision => false;
            public int? Serial => eewUpdates;
            public bool IsComparableWith(IReportRevisionKey other)
            {
                if (other is UpdatesKey)
                    return true;
                return false;
            }
            public UpdatesKey(int eewUpdates)
            {
                this.eewUpdates = eewUpdates;
            }
        }
        
        public sealed record EventIDGroupKey:IReportUnitKey
        {
            private string EventId { get; init; }
            public EventIDGroupKey(string eventId)
            {
                EventId = eventId;
            }
        }
        
        public ReportModel Generate(object e, IReportGeneratorContext? context, ref CultureInfo culture) {
            ThrowHelper.ThrowIfNull(e);
            ReportModel result = new ReportModel();
            if (e is EEWEntry)
            {
                EEWEntry entry = (EEWEntry)e;
                result.Title = Worker.lastInstance.eewcnJs.GetString("ReportTitleEEW");
                result.Source=Worker.lastInstance.eewcnJs.GetString("ReportSource");
                result.RevisionKey = new UpdatesKey(entry.updates);
                result.Location = entry.epicenter;
                result.Predicate=Worker.lastInstance.eewcnJs.GetString("EEWReportOccur");
                result.Time = DateTimeOffset.FromUnixTimeMilliseconds(entry.startAt);
                result.TimeZone=TimeZoneInfo.Local;
                result.InvalidatedTime = Util.CalcLiveTimeTo(entry.startAt,entry.magnitude,entry.depth);
                int maxInt=Util.CalcMaxInt(entry.magnitude,entry.depth);
                //Severity是个啥？
                result.Properties.Add(new ReportProperty(TagTypeKeys.Intensity,Worker.lastInstance.eewcnJs.GetString("ReportHypoInt"),maxInt.ToString(),maxInt));
                result.Properties.Add(new ReportProperty(TagTypeKeys.Magnitude,Worker.lastInstance.eewcnJs.GetString("ReportMagnitude"),entry.magnitude.ToString(),entry.magnitude));
                result.Properties.Add(new ReportProperty(TagTypeKeys.HypocenterDepth,Worker.lastInstance.eewcnJs.GetString("ReportDepthColon"),((int)entry.depth)+"km",entry.depth));
                result.GroupKeys.Add(new EventIDGroupKey(entry.eventId));
            }
            else
            {
                HistoryEntry entry=(HistoryEntry)e;
                result.Title = Worker.lastInstance.eewcnJs.GetString("ReportTitleHistory");
                result.Source=Worker.lastInstance.eewcnJs.GetString("ReportSource");
                result.RevisionKey = new UpdatesKey(entry.AUTO_FLAG == "M" ? 1 : 0);
                result.Location = entry.LOCATION_C;
                result.Predicate=Worker.lastInstance.eewcnJs.GetString("HistoryReportOccur");
                result.Time = DateTimeOffset.Parse(entry.O_TIME.ToUpper().Replace("UTC", ""));
                result.TimeZone = TimeZoneInfo.Local;
                int maxInt = Util.CalcMaxInt(float.Parse(entry.M), entry.EPI_DEPTH);
                //Severity是个啥？
                result.Properties.Add(new ReportProperty(TagTypeKeys.Intensity,Worker.lastInstance.eewcnJs.GetString("ReportHypoInt"),maxInt.ToString(),maxInt));
                result.Properties.Add(new ReportProperty(TagTypeKeys.Magnitude,Worker.lastInstance.eewcnJs.GetString("ReportMagnitude"),entry.M,Convert.ToSingle(entry.M)));
                result.Properties.Add(new ReportProperty(TagTypeKeys.HypocenterDepth,Worker.lastInstance.eewcnJs.GetString("ReportDepthColon"),((int)entry.EPI_DEPTH)+"km",entry.EPI_DEPTH));
                result.GroupKeys.Add(new EventIDGroupKey(entry.id));
            }

            return result;
        }
    }
}