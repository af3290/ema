using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MarketModels;
using static MarketModels.MathFunctions;
using static MarketModels.Preprocess;
using static MarketModels.Simulations;
using EMA.Misc;
using MathNet.Numerics.Distributions;

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
            if (timeStepsInLevels.Length != priceLevels.Length)
                throw new Exception("Lengths not consistent");

            //hourly simulation
            var horizon = timeStepsInLevels.Sum()*24; //
            
            //TODO: add daily/hourly resolution... if it makes sense... but not really.
            
            //estimate an hourly ARMA...
            var d = AppData.GetHistoricalSeries(PricesController._timeSeries);

            var data = d
                .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..
                .ToArray();

            //just take last 3 years... mhm...
            var threeYears = 24 * 7 * 4 * 12 * 3;
            if (data.Length > threeYears)
            {
                data = data.Skip(data.Length - threeYears).Take(threeYears).ToArray();
            }
            //detrend to make stationary
            data = TimeSeries.toStationary(data);

            //hourly
            //var seasons = new int[] { 24, 24 * 7, 24 * 7 * 4, 24 * 7 * 4 * 6, 24 * 7 * 4 * 12};
            //var arSes = new int[] { 1, 2, 24, 25 };

            //daily
            //var seasons = new int[] { 7, 7*4, 7*4*12 };
            //var arSes = new int[] { 1, 7, 7*4 };

            //var arma = TimeSeries.ARMASimple2(data, arSes, seasons);

            //fitted model from matlab
            var arlags = new int[] { 1, 2, 24, 25 };
            //var ar = new TimeSeries.LagOp(new double[] { 1.04216, -0.125323, 0.777886, -0.699448 }, arlags);
            var malags = new int[] { 24, 168 };
            //var ma = new TimeSeries.LagOp(new double[] { -0.44595, 0.175135 }, malags);
            //var xarma = TimeSeries.ARMASimple3(ar, ma, 0.166685, 2.80023);

            //our fitting procedure.. difference...? not much...
            var arma = TimeSeries.ARMASimple2(data, arlags, malags);
            arma = TimeSeries.ARMASimple3(arma.AR, arma.MA, arma.Const, volatility);

            var inSampleRes = TimeSeries.Infer(arma, data);

            var sims = arma.Simulate(numberOfSimulations, horizon, data, inSampleRes);
                        
            var hoursSteps = timeStepsInLevels.Select(x => x * 24).ToArray();

            var desezonalizedData = Desezonalize(data, 168);

            //add the spikes
            //Then perform the spike estimation 
            var spikesThreshold = 1.7;
            var spikeIndices = EstimateSpikesOnMovSDs(desezonalizedData, 24, 2, spikesThreshold);

            //select hour
            var peakHour = 16;
            var peakhourData = TakeShortPeriods(data, 1, peakHour, 24);
            var peakSpikeIndices = EstimateSpikesOnMovSDs(peakhourData, 7, 2, spikesThreshold);
            var distrib = EstimateSpikesDistribution(peakhourData, peakSpikeIndices, Forecast.SpikePreprocess.SimilarDay, spikesThreshold);

            //for testing..
            var n = new Normal(distrib.Item1.Mean, distrib.Item1.Variance);
            var p = new Poisson(distrib.Item2.Lambda * 24);

            //replace spikes now...
            //TODO: replace at the right hour... yes...
            sims = Simulations.spikeSimulationsReplace(sims, p, n);

            //fit the forward curve
            sims = Simulations.liftSeriesToPriceCurve(sims, hoursSteps, priceLevels);

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

            var sims = spotPriceSimulationsConfidence(timeStepsInLevels, priceLevels,
                timeStep, reversionRate, volatility, alpha);

            var hoursSteps = timeStepsInLevels.Select(x => x * 24).ToArray();
            var means = dailyLongTermMean(hoursSteps, priceLevels);

            //TODO: FINISH HIS SHIT! it's really awesome!
            var res = new
            {
                ConfidenceIntervals = sims,
                ForwardInterpolation = means
            };

            return Json(res);
        }

        [HttpPost]
        public JsonResult ForwardCurveSimulations(string curve, DateTime? date, int forwardSteps, string method,
            int timeHorizon, int numberSimulations, string otherParams) //such as seasonalities... etc...
        {
            DateTime dt = date ?? new DateTime(DateTime.Now.Year - 1, DateTime.Now.Month, DateTime.Now.Day);

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

            //eliminate nulls if any...
            var fwdCurve = x
                .Where(ts=>ts.Prices.Count(tsValue=>tsValue.DateTime == date) == 1)
                .Select(ts => (double)ts.Prices.Single(p => p.DateTime == date).Value).ToArray();

            var sims = Simulations.curveSimulationsHistorical(fwdCurve, data, numberSimulations, 1.0 / 360.0);

            return Json(sims, JsonRequestBehavior.AllowGet);
        }

        ///COPY PASTE ALERT!
        [HttpPost]
        public JsonResult ForwardCurveConfidence(string curve, DateTime? date, int forwardSteps, string method,
            int timeHorizon, double confidence, string otherParams) //such as seasonalities... etc...
        {
            DateTime dt = date ?? new DateTime(DateTime.Now.Year - 1, DateTime.Now.Month, DateTime.Now.Day);

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

            //eliminate nulls if any...
            var fwdCurve = x
                .Where(ts => ts.Prices.Count(tsValue => tsValue.DateTime == date) == 1)
                .Select(ts => (double)ts.Prices.Single(p => p.DateTime == date).Value).ToArray();

            var confs = Simulations.curveSimulationsHistoricalConfidence(fwdCurve, data, confidence, 1.0 / 360.0);

            var res = new
            {
                ConfidenceLevels = new double[] { confidence },
                //from highest to lowest prediction intervals
                Confidence = confs
            };

            return Json(res, JsonRequestBehavior.AllowGet);
        }
    }
}