using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class PriceCurvesSensitivity
    {
        private PriceCurvesModel _pcm;
        private decimal _prc;

        public PriceCurvesSensitivity(PriceCurvesModel pcm, decimal prc)
        {
            _pcm = pcm;
            _prc = prc;
        }

        private decimal SupplyCurvePriceSensitivityByPrc(List<MarketPoint> curve, decimal prc)
        {
            var e = _pcm.Equilibrium;

            if (e == null)
                return -1;

            var deltaVol = e.Volume * prc;
            var newVol = e.Volume * (1 + prc);

            /* Find containing line, shouldn't contain nulls at all!!! TODO: fix later */
            var start = curve.LastOrDefault(s => s.Volume < newVol);
            start = start ?? curve.First();
            var end = curve.FirstOrDefault(s => newVol < s.Volume);
            end = end ?? curve.Last();

            var slope = Line.FindSlope(start.Volume, start.Price, end.Volume, end.Price);

            var deltaPrice = slope * deltaVol;

            return deltaPrice;
        }

        //Sensitivities of supply, ... YES!
        public decimal PriceDeltaSupplyMinusPrc => SupplyCurvePriceSensitivityByPrc(_pcm.SupplyCurve, -_prc);

        public decimal PriceDeltaDemandMinusPrc => SupplyCurvePriceSensitivityByPrc(_pcm.DemandCurve, -_prc);

        public decimal PriceDeltaSupplyPlusPrc => SupplyCurvePriceSensitivityByPrc(_pcm.SupplyCurve, _prc);

        public decimal PriceDeltaDemandPlusPrc => SupplyCurvePriceSensitivityByPrc(_pcm.DemandCurve, _prc);

        public decimal PercentageChange { get { return _prc * 100; } set { _prc = value / 100; } }
    }
}
