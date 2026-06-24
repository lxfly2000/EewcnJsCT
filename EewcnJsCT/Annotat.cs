using System.Globalization;
using Cryville.Common.Compat;
using Cryville.EEW;
using Cryville.EEW.Features;
using Cryville.EEW.TTS;

namespace EewcnJsCT
{
    public class Annotat:IGenerator<object,Feature>,IPropertiesHolder
    {
        public bool MyConfig { get; set; }

        public Annotat(bool myConfig)
        {
            MyConfig = myConfig;
        }
        public Feature Generate(object e, ref CultureInfo culture) {
            ThrowHelper.ThrowIfNull(e);

            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return new Feature();
        }
    }
}