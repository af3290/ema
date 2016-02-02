using MarketModels;
using Newtonsoft.Json;
using EMA.DomainModels;
using EMA.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Utils;
using static MarketModels.Types;
using static MarketModels.MathFunctions;

namespace EMA.Controllers
{
    public class PlotlyController : Controller
    {
        public ActionResult SpotMarketCurvesSurface(DateTime? date,
            decimal sensitivityChangePercentage, string EquilibriumAlgorithm, string EquilibriumFill)
        {
            var dt = date.HasValue ? date.Value : DateTime.Today;
            var curves = AppData.GetNordpoolMarketCurves(dt);
        
            curves.ForEach(c => {
                c.EqulibriumAlgorithm = GetUnionCaseFromName<EquilibriumAlgorithm>(EquilibriumAlgorithm);
                c.EquilibriumFill = GetUnionCaseFromName<EquilibriumFill>(EquilibriumFill);
                c.CalculateEquilibrium();
                c.Sensitivity.PercentageChange = sensitivityChangePercentage;
            });

            /* Need to reverse to match the plotly structure*/
            //curves.Reverse();
            //curves.ForEach(c => c.SupplyCurve.Reverse());
            //curves.ForEach(c => c.DemandCurve.Reverse());

            /* Piece together all plot ribbon js options */
            var plotData = new List<string>();

            string plotOptions = "{{\"showscale\": false, \"x\": {0}, \"y\": {1}, \"z\": {2}, \"type\": \"surface\", \"name\": \"{3}\"}}";

            /* Transform them for plotly ribbons model... */
            var supplyData = PreparePlotlyModelSurface(curves.Select(c => c.SupplyCurve).ToList());
            plotData.Add(string.Format(plotOptions, supplyData.X, supplyData.Y, supplyData.Z, "Supply"));

            var demandData = PreparePlotlyModelSurface(curves.Select(c => c.DemandCurve).ToList());
            plotData.Add(string.Format(plotOptions, demandData.X, demandData.Y, demandData.Z, "Demand"));

            var json = "[" + string.Join(", ", plotData) + "]";

            ViewBag.ViewModel = JsonConvert.SerializeObject(curves);
            ViewBag.PlotData = json;

            return View();
        }

        private PlotlyJsonSurfaceCorrdinate PreparePlotlyModelSurface(List<List<MarketPoint>> curves)
        {
            /* Add hours as the 3rd coordinate */
            var coords = curves.Select((c, hour) => c.Select(
                x => new SurfaceCoordinate()
                {
                    X = x.Volume,
                    Y = x.Price,
                    Z = hour
                }
            ).ToList()).ToList();

            /* Separate coordinates in distinct matrices */
            var X = coords.Select(coord => coord.Select(c => c.X).ToArray()).ToArray();
            var Y = coords.Select(coord => coord.Select(c => c.Y).ToArray()).ToArray();
            var Z = coords.Select(coord => coord.Select(c => c.Z).ToArray()).ToArray();

            return new PlotlyJsonSurfaceCorrdinate()
            {
                /* Switch axis to show properly */
                X = JsonConvert.SerializeObject(X),
                Y = JsonConvert.SerializeObject(Z),
                Z = JsonConvert.SerializeObject(Y),
            };
        }
        
        public ActionResult ProbabilitySurface(string series, int bins, int period, int? dayOfWeekNo, 
            string distributionType)
        {
            var d = AppData.GetHistoricalSeries("SystemPrice_Hourly_EUR.json");
            var data = d.Select(s => (double)s.Value).ToArray();

            /* When doing daily PDF provide specific day PDF instead of all days... */
            if(dayOfWeekNo.HasValue && 1 <= dayOfWeekNo && dayOfWeekNo <= 7 && period == 24)
            {
                data = TakeShortPeriods(data, period, dayOfWeekNo.Value - 1, 168);
            }

            double[,] surf;

            if(string.Equals(distributionType, "LogNormal"))
                surf = SeasonalProbabilityDensities(data, period, bins, HistogramFit.LogNormal);
            else if(string.Equals(distributionType, "Normal"))
                surf = SeasonalProbabilityDensities(data, period, bins, HistogramFit.Normal);
            else
                surf = SeasonalProbabilityDensities(data, period, bins, HistogramFit.None);
                       

            /* Piece together all plot ribbon js options */
            var plotData = new List<string>();

            string plotOptions = "{{\"showscale\": false, \"x\": {0}, \"y\": {1}, \"z\": {2}, \"type\": \"surface\", \"name\": \"{3}\"}}";

            //TODO: add mean level back...  set other coordinates right...
            /* Transform them for plotly ribbons model... */
            var supplyData = PreparePlotlyModelSurface(surf);
            plotData.Add(string.Format(plotOptions, supplyData.X, supplyData.Y, supplyData.Z, "Probability Distribution Function Surface"));
            
            var json = "[" + string.Join(", ", plotData) + "]";

            ViewBag.ViewModel = JsonConvert.SerializeObject(surf);
            ViewBag.PlotData = json;

            return View();
        }

        private PlotlyJsonSurfaceCorrdinate PreparePlotlyModelSurface(double[,] data)
        {
            /* Add hours as the 3rd coordinate */
            var coords = new List<List<SurfaceCoordinate>>();

            for (int i = 0; i < data.GetLength(0); i++)
            {
                var c = new List<SurfaceCoordinate>();

                for (int j = 0; j < data.GetLength(1); j++)
                {
                    c.Add(new SurfaceCoordinate()
                    {
                        X = data.GetLength(0) - i - 1, //to avoid inversion...
                        Y = (decimal)data[i,j],
                        Z = j
                    });
                }
                coords.Add(c);
            }

            //why i s it reversed???
            coords.Reverse();

            /* Separate coordinates in distinct matrices */
            var X = coords.Select(coord => coord.Select(c => c.X).ToArray()).ToArray();
            var Y = coords.Select(coord => coord.Select(c => c.Y).ToArray()).ToArray();
            var Z = coords.Select(coord => coord.Select(c => c.Z).ToArray()).ToArray();

            return new PlotlyJsonSurfaceCorrdinate()
            {
                /* Switch axis to show properly */
                X = JsonConvert.SerializeObject(X),
                Y = JsonConvert.SerializeObject(Z),
                Z = JsonConvert.SerializeObject(Y),
            };
        }
    }
}