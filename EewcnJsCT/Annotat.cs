using System;
using System.Globalization;
using Cryville.Common.Compat;
using Cryville.EEW;
using Cryville.EEW.Features;
using Cryville.Measure;
using static Cryville.EEW.TagTypeKeys;

namespace EewcnJsCT
{
    public class Annotat:IGenerator<object,Feature>,IPropertiesHolder
    {
        public Annotat()
        {
        }
        public Feature Generate(object e, ref CultureInfo culture) {
            ThrowHelper.ThrowIfNull(e);
            Feature result = new Feature();
            if (e is EEWEntry)
            {
                EEWEntry entry = (EEWEntry) e;
                DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds(entry.startAt);
                result = new Feature()
                {
                    { Is, Earthquake },
                    { Ongoing, true },
                    { Time, dto },
                    {
                        At, new Feature(new Point(entry.longitude, entry.latitude))
                        {
                            { Is, Hypocenter },
                            { Name, entry.epicenter },
                            { HypocenterDepth, new Quantity(entry.depth * 1000, Units.Metre) }
                        }
                    },
                    { Magnitude, new Quantity(entry.magnitude, Units.Dimensionless) },
                    { TimeModified, new DateTimeOffset(DateTime.Now.Ticks, TimeSpan.Zero) },
                };
            }
            else
            {
                HistoryEntry entry = (HistoryEntry) e;
                DateTimeOffset dto = DateTimeOffset.Parse(entry.O_TIME.ToUpper().Replace("UTC", ""));
                result = new Feature()
                {
                    { Is, Earthquake },
                    { Time, dto },
                    {
                        At, new Feature(new Point(Convert.ToDouble(entry.EPI_LON), Convert.ToDouble(entry.EPI_LAT)))
                        {
                            { Is, Hypocenter },
                            { Name, entry.LOCATION_C },
                            { HypocenterDepth, new Quantity(entry.EPI_DEPTH * 1000, Units.Metre) }
                        }
                    },
                    { Magnitude, new Quantity(Convert.ToDouble(entry.M), Units.Dimensionless) },
                    { TimeModified, new DateTimeOffset(DateTime.Now.Ticks, TimeSpan.Zero) },
                };
            }

            return result;
        }
    }
}