using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MarketModels;
using EMA.Misc;

namespace EMA.Controllers
{
    public class SimulationsController : Controller
    {
        //add curveIds to show what we're referring to...
        [HttpPost]
        public JsonResult SpotPriceSimulation(
            int[] timeStepsInLevels, double[] priceLevels, 
            double timeStep, double reversionRate, double volatility,
            int numberOfSimulations)
        {
            if(timeStepsInLevels.Length != priceLevels.Length)
                throw new Exception("Lengths not consistent");

            //etc...

            var sims = Simulations.spotPriceSimulations(timeStepsInLevels, numberOfSimulations, priceLevels,
                timeStep, reversionRate, volatility);

            return Json(sims);
        }

        [HttpPost]
        public JsonResult SpotPriceConfidence(
            int[] timeStepsInLevels, double[] priceLevels,
            double timeStep, double reversionRate, double volatility,
            double alpha)
        {
            if (timeStepsInLevels.Length != priceLevels.Length)
                throw new Exception("Lengths not consistent");

            //very simple: instead of putting in random numbers for each steps, 
            //we put the specific values

            var sims = Simulations.spotPriceSimulationsConfidence(timeStepsInLevels, priceLevels,
                timeStep, reversionRate, volatility, alpha);

            //TODO: FINISH HIS SHIT! it's really awesome!

            return Json(sims);
        }

        [HttpPost]
        public JsonResult ForwardCurveSimulations(string curve, DateTime? date, int forwardSteps, string method,
            string otherParams) //such as seasonalities... etc...
        {
            DateTime dt = date ?? new DateTime(DateTime.Now.Year-1, DateTime.Now.Month, DateTime.Now.Day);
            
            if (curve != "Natural-Gas-Futures-NYMEX")
                throw new ArgumentException("Not valid cuve");

            //data is re-fetched, so nothing big is actually posted...
            var x = AppData.GasForwards()
                .OrderBy(ts => ts.Maturity)
                .ToList();
            
            x = x.Where(t => dt < t.Maturity && t.Maturity <= dt.AddMonths(forwardSteps))
                .ToList();
            //x = x.Where(t => t.Prices.First().DateTime <= date && date <= t.Prices.Last().DateTime).ToList();

            //do covariance on historical data...
            var data = x.Select(ts => ts.Prices
                            .Select(p => p.Value != null ? (double)p.Value : 0) //0 for now, when interpolation is done... etc..
                            .ToArray())
                        .ToArray();
            //assumes no nulla in data
            var fwdCurve = x.Select(ts => (double)ts.Prices.SingleOrDefault(p => p.DateTime == date).Value).ToArray();

            var sims = Simulations.curveSimulationsHistorical(fwdCurve, data, 5, 1.0 / 360.0);
            
            return Json(sims, JsonRequestBehavior.AllowGet);
        }
    }
}