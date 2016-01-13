using System;
using System.Collections.Generic;
using System.Linq;
using MarketModels;
using static MarketModels.Types;

namespace Utils
{
    public class PriceCurvesModel
    {
        private MarketPoint _equilibrium;
        private PriceCurvesSensitivity _pcs;

        public PriceCurvesModel()
        {
            DemandCurve = new List<MarketPoint>();
            SupplyCurve = new List<MarketPoint>();
        }

        public int Hour;

        /// <summary>
        /// Already ordered increasingly by price. 
        /// May contain a lot of equal Price values.
        /// </summary>
        public List<MarketPoint> DemandCurve;

        /// <summary>
        /// Already ordered increasingly by price. 
        /// May contain a lot of equal Price values.
        /// </summary>
        public List<MarketPoint> SupplyCurve;

        /// <summary>
        /// Also, it is available from nordpoolspot, but it can be internally calculated through 
        /// several interpolation and intersection algorithms (and also using welfare maximization).
        /// </summary>
        public MarketPoint Equilibrium
        {
            get
            {
                if (_equilibrium != null)
                    return _equilibrium;
                
                CalculateEquilibrium();

                return _equilibrium;
            }
        }

        public PriceCurvesSensitivity Sensitivity
        {
            get
            {
                /* No equilibrium => no sensitivities */
                if (_equilibrium == null)
                    return null;

                return _pcs ?? (_pcs = new PriceCurvesSensitivity(this, 0.01m));
            }
        }

        public void CalculateEquilibrium()
        {
            /* Find bounding box */
            var maxMinVolume = Math.Max(DemandCurve.Min(d => d.Volume), SupplyCurve.Min(d => d.Volume));
            var minMaxVolume = Math.Min(DemandCurve.Max(d => d.Volume), SupplyCurve.Max(d => d.Volume));

            var maxMinPrice = Math.Max(DemandCurve.Min(d => d.Price), SupplyCurve.Min(d => d.Price));
            var minMaxPrice = Math.Min(DemandCurve.Max(d => d.Price), SupplyCurve.Max(d => d.Price));

            var subDemandCurve = DemandCurve
                .Where(d => maxMinVolume <= d.Volume && d.Volume <= minMaxVolume)
                .Where(d => maxMinPrice <= d.Price && d.Price <= minMaxPrice)
                .ToList();
            var subSupplyCurve = SupplyCurve
                .Where(d => maxMinVolume <= d.Volume && d.Volume <= minMaxVolume)
                .Where(d => maxMinPrice <= d.Price && d.Price <= minMaxPrice)
                .ToList();

            /* Start aproximating */
            var firstDemandPoint = subDemandCurve.First();
            var lastDemandPoint = subDemandCurve.Last();

            //better version: LP program
            //=> has a very odd shaping for peak hours... WHY?
            //CalculateEquilibriumLP(subDemandCurve, subSupplyCurve);

            //considerably less work, but still inefficient
            CalculateEquilibriumBruteForce(subDemandCurve, subSupplyCurve);
        }

        public void CalculateEquilibriumLP(List<MarketPoint> demandCurve, List<MarketPoint> supplyCurve)
        {
            var equilQ = MarketClearing.FindEquilibrium(supplyCurve.ToRectangularArray(), demandCurve.ToRectangularArray());

            _equilibrium = new MarketPoint()
            {
                Price = (decimal)equilQ.eqPrice,
                Volume = (decimal)equilQ.eqQuantity
            };
        }

        public void CalculateEquilibriumPolynomsIntersection(List<MarketPoint> demandCurve, List<MarketPoint> supplyCurve)
        {

        }

        //TODO: refactor to a more efficient algorithm! TOO SLOW... WAY TOO SLOW...
        public void CalculateEquilibriumBruteForce(List<MarketPoint> demandCurve, List<MarketPoint> supplyCurve)
        {
            /* Brute force calculation for now */
            var distances = new List<Tuple<int,int,double>>();
            for (int i = 0; i < demandCurve.Count; i++)
            {
                var dPoint = demandCurve[i];

                for (int j = 0; j < supplyCurve.Count; j++)
                {
                    var sPoint = supplyCurve[j];

                    var l = Line.FindLength(dPoint.Volume, dPoint.Price, sPoint.Volume, sPoint.Price);
                    var t = new Tuple<int, int, double>(i, j, l);
                    distances.Add(t);
                }
            }
            
            var smallestDistance = distances
                .OrderBy(d=>d.Item3)
                .First();
            var secondSmallestDistance = distances
                .Where(d=>d.Item3 > smallestDistance.Item3)
                .OrderBy(d => d.Item3)
                .First();

            var iStart = smallestDistance.Item1;
            var iEnd = secondSmallestDistance.Item1;
            //should not happen
            //DemandStart is closer to supply since Demand is downward sloping... so it's ordered decreasingly...
            if (iStart == iEnd && iStart > 1)
                iEnd = iStart - 1;

            var jStart = smallestDistance.Item2;
            var jEnd = secondSmallestDistance.Item2;
            //should not happen, 
            
            if (jStart == jEnd && jStart < SupplyCurve.Count - 1)
                jEnd = jStart + 1;

            /* Interpolate to find equilibrium values */
            var demandLine = new Line(
                demandCurve[iStart].Volume, demandCurve[iStart].Price,
                demandCurve[iEnd].Volume, demandCurve[iEnd].Price);

            var supplyLine = new Line(
                supplyCurve[jStart].Volume, supplyCurve[jStart].Price,
                supplyCurve[jEnd].Volume, supplyCurve[jEnd].Price);

            Point<decimal> intersection = demandLine.Intersect(supplyLine);

            _equilibrium = new MarketPoint()
            {
                Price = intersection.Y,
                Volume = intersection.X
            };
        }
        
        public decimal NetFlow;
        public MarketBlock Accepted, NonAccepted;
        
    }
}