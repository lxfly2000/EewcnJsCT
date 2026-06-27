using System;

namespace EewcnJsCT
{
    public class Value
    {
        public static string timestampMSToChineseDateTime(long timestamp)
        {
            DateTimeOffset dt= DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            string isoFmt = dt.ToLocalTime().ToString("O");
            return isoFmt.Substring(0, 10) + " " + isoFmt.Substring(11, 8);
        }

        public static long chineseDateTimeToTimestampMS(string fmt)
        {
            DateTimeOffset dt = DateTimeOffset.Parse(fmt);
            return dt.ToUnixTimeMilliseconds();
        }
    }
}