using Cryville.Common.Compat;
using Cryville.EEW;
using Cryville.EEW.Report;
using System;
using System.Globalization;

namespace EewcnJsCT {
    public class Reporter : IContextedGenerator<object, IReportGeneratorContext, ReportModel>, IPropertiesHolder {
        public bool MyConfig { get; set; }

        public Reporter(bool myConfig)
        {
            MyConfig = myConfig;
        }
        public ReportModel Generate(object e, IReportGeneratorContext? context, ref CultureInfo culture) {
            ThrowHelper.ThrowIfNull(e);
            context ??= EmptyReportGeneratorContext.Instance;

            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            var result = new ReportModel {
                Title = "Title",
                Source = "Source",
                Location = "LOCATION",
                Time = DateTimeOffset.Now,
                TimeZone = TimeZoneInfo.Utc,
            };
            //result.GroupKeys.Add(/* ... */);
            //result.Properties.Add(/* ... */);

            return result;
        }
    }
}