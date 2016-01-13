using System;
using static MarketModels.Types;

namespace Utils.NordPoolSpot
{
    public static class Extensions
    {
        public static string ToXString(this Country me)
        {
            if (me == Country.All)
                return "per-country";
            return me.ToString();
        }

        public static string ToXString(this DataItem me)
        {
            var n = me.ToString();
            var val = n.Replace("_", "-");

            return val;
        }

        public static string ToXString(this Resolution me)
        {
            return GetUnionCaseName(me);
        }
    }
}