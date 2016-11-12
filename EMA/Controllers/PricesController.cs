using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Utils;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using static MarketModels.Types;
using static MarketModels.Forecast;
using static MarketModels.TimeSeries;
using static MarketModels.Preprocess;
using EMA.Misc;
using MarketModels;
using Newtonsoft.Json.Linq;

namespace EMA.Controllers
{
    public class PricesController : Controller
    {
        /* Nordpoolspot */
        public static string _timeSeries = "SystemPrice_Hourly_EUR.json";
        //figure out a way to change it as runtime, yeah... with an URL parameter...
        //public static string _timeSeries = "JPX_Price_Hourly.json";
        private string _exVarsPre = @"";

        /* Weron California prices... */
        //private string _timeSeries = @"CA\Price_Hourly.json";
        //private string _exVarsPre = @"CA\";

        //TODO: add uniqueStructure only for non overlapping short contracts to take priority
        public JsonResult ForwardCurve(string region, bool nonOverlapping)
        {
            var dd = new Utils.NasdaqOMX.Downloader();
            var fc = dd.ForwardCurve(nonOverlapping);
            return Json(fc, JsonRequestBehavior.AllowGet);
        }

        public JsonResult HistoricalSystemPrice(bool refresh, string resolution, string timeSeries)
        {
            //until the rest is updated... don't use it
            //if(timeSeries == "JPX")
            //{
            //    var h = AppData.GetHistoricalSeries("JPX_Price_Hourly.json");
            //    return Json(h, JsonRequestBehavior.AllowGet);
            //}

            if (resolution == "5")//because of bullshit F# types... dummy for now... fix later
            {
                //TODO: fix here... add more demos, yeah!
                var h = AppData.GetHistoricalSeries(_timeSeries);
                var off = h.Count(p => p.Value < 0 || p.Value > 500);
                var nans = h.Count(p => !p.Value.HasValue);
                return Json(h, JsonRequestBehavior.AllowGet);
            }

            //TODO: aggregate to daily... does it make sense?
            var dailyHistoricalPrices = AppData.GetHistoricalSeriesDaily(_timeSeries);

            return Json(dailyHistoricalPrices, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GasFutures(DateTime? date, int forwardSteps)
        {
            var x = AppData.GasForwards()
                .OrderBy(ts => ts.Maturity)
                .ToList();

            var dt = date.HasValue ? date.Value : DateTime.Today;

            DateTime afterMaturity = new DateTime(dt.Year, dt.Month, 1).AddMonths(1);
            DateTime beforeMaturity = new DateTime(dt.Year, dt.Month, 1).AddMonths(forwardSteps);

            //typically the next month after last trading datetime is the maturity
            x = x.Where(t => afterMaturity <= t.Maturity && t.Maturity <= beforeMaturity).ToList();

            //all futures that have data at the available at datetime, all futures which represent maturities to this spot datetime
            x = x.Where(t => t.Prices.First().DateTime <= date && date <= t.Prices.Last().DateTime).ToList();                
            
            return Json(x, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LiveNordicProductionConsumption()
        {
            /* Convert to norway's time zone */
            var date = DateTime.UtcNow; //.AddHours(-1)

            /* Aproximate to latest minute */
            date = date.AddSeconds(-DateTime.Now.Second);
            var timestamp = date.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            timestamp = timestamp / 10000;

            var url = @"http://driftsdata.statnett.no/restapi/ProductionConsumption/GetLatestDetailedOverview?timestamp={0}";
            var furl = string.Format(url, timestamp);
            var result = new WebClient().DownloadString(furl);

            /* Proper parsing */
            result = result.Replace("&nbsp", "").Replace("Â", "");
            var ms = Regex.Matches(result, @"""[\d\s]+""");
            foreach (var m in ms)
            {
                var newValue = m.ToString().Replace(" ", "").Replace("\"", ""); ;
                result = result.Replace(m.ToString(), newValue);
            }

            var obj = JsonConvert.DeserializeObject(result);

            return Json(obj, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult EstimateSpikes(string timeSeries, string spikesPreprocessMethod, double spikesThreshold)
        {
            var spikesPreprocess = GetUnionCaseFromName<SpikePreprocess>(spikesPreprocessMethod);

            //Build output by re attaching the corresponding original dates...
            var spikes = new List<HistoricalPrice>();
            var interpolation = new List<HistoricalPrice>();

            if (spikesPreprocess != SpikePreprocess.None)
            {

                var d = AppData.GetHistoricalSeries(_timeSeries);

                var data = d
                    .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..
                    .ToArray();

                //First deseasonalize data at larger lags, all >= weekly, a stable series is need since spikes are at lengths <= day
                var desezonalizedData = Desezonalize(data, 168);

                //Then perform the spike estimation 
                var spikeIndices = EstimateSpikesOnMovSDs(desezonalizedData, 24, 2, spikesThreshold);
                
                for (int i = 0; i < spikeIndices.Length; i++)
                {
                    var p = d[spikeIndices[i]];
                    spikes.Add(new HistoricalPrice() { DateTime = p.DateTime, Value = p.Value });
                }

                if(spikesPreprocess.IsLimited)
                {
                    //limits... interpolate between clusters... yeah...
                }

                if (spikesPreprocess.IsSimilarDay)
                {
                    //last week's corresponding day shown
                    
                }
            }

            var obj = new
            {
                Spikes = spikes,
                Interpolation = interpolation
            };

            return Json(obj);
        }

        [HttpPost]
        public JsonResult Forecast(DateTime? date, string forecastMethod, string spikesPreprocessMethod, double spikesThreshold, 
            string forecastModel, string timeHorizon, double confidence, string MathModel, string exogenousVariables)
        {
            var dt = date.HasValue ? date.Value : DateTime.Today;
            var ths = GetUnionCaseNames<Types.TimeHorizon>();

            var forecastHorizon = Array.IndexOf(ths, timeHorizon);
            var forecastingMethod = GetUnionCaseFromName<ForecastMethod>(forecastMethod);
            
            if (forecastHorizon < 0)
                throw new ArgumentException("Wrong argument");

            //naive hardcoded for now...
            //data to estimate on is not an input parameter...
            var d = AppData.GetHistoricalSeries(_timeSeries);

            var data = d.Where(x => x.DateTime < dt)
                .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..
                .ToArray();

            var spikesPreprocess = GetUnionCaseFromName<SpikePreprocess>(spikesPreprocessMethod);

            if(spikesPreprocess.IsSimilarDay)
            {
                //First deseasonalize data at larger lags, all >= weekly, a stable series is need since spikes are at lengths <= day
                var desezonalizedData = Desezonalize(data, 168);

                //Then perform the spike estimation 
                var spikeIndices = EstimateSpikesOnMovSDs(desezonalizedData, 24, 2, spikesThreshold);

                //go ahead and pre process the d series... yeah...
                data = ReplaceSingularSpikes(data, spikeIndices, spikesPreprocess, 0.95);
            }

            var horizon = GetTimeHorizonValue(forecastHorizon);

            var rlzd = d.Where(x => x.DateTime >= dt)
                .Take(horizon)
                .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..                
                .ToArray();

            ForecastResult forecast;
            double[] forecasted;
            object model;

            var oneYear = (int)Math.Floor((dt - dt.AddMonths(-12)).TotalHours);
            var halfYear = (int)Math.Floor((dt - dt.AddMonths(-6)).TotalHours);

            var seasons = new int[] { oneYear, halfYear, 24 * 7 * 4 * 3, 24 * 7 * 4, 24 * 7, 24 };

            //we need always seasonalities >= forecast horizon
            var relevantSeasons = seasons.Where(s => s >= horizon).ToArray();

            double[] estimationData = null;

            //add naive with exogenous variables next...
            if (forecastingMethod == ForecastMethod.Naive)
            {
                forecast = Naive(data, relevantSeasons, horizon, confidence);

                forecasted = forecast.Forecast.Take(rlzd.Length).ToArray();

                model = new object();
            }
            else if (forecastingMethod == ForecastMethod.HoltWinters)
            {
                estimationData = data.Reverse().Take(24 * 7 * 2).Reverse().ToArray();

                var hwparams = JsonConvert.DeserializeObject<HoltWinters.HoltWintersParams>(MathModel);

                //optimize if all seem to be wrong
                if (hwparams.alpha == 0 || hwparams.beta == 0 || hwparams.gamma == 0)
                    hwparams = HoltWinters.OptimizeTripleHWT(estimationData, 24, horizon);

                forecast = HoltWinters.TripleHWTWithPIs(estimationData, 24, horizon, hwparams, confidence);

                forecasted = forecast.Forecast.Take(rlzd.Length).ToArray();

                model = hwparams;
            }
            else if (forecastingMethod == ForecastMethod.ARMA)
            {
                var exogenousVariablesJs = JObject.Parse(exogenousVariables); //alternative...?

                //always take next 2 highest seasons...
                var maSes = seasons.Where(s => s >= horizon).OrderBy(x => x).Take(2).ToArray();
                var arSes = new int[] { 1, 2, 24, 25 };

                //twice as the highest season estimation data
                var estimationDataLength = maSes.Last() * 2;
                estimationData = data.Reverse().Take(estimationDataLength).Reverse().ToArray();

                var isUnivariate = exogenousVariablesJs.Properties().Where(p => p.Value.ToString() == "True").Count() == 0;

                if (isUnivariate)
                {
                    var arma = JsonConvert.DeserializeObject<ARMAResult>(MathModel);

                    //problem: when changing param for ARMA, provide forecast, when changed date or horizon, re-estimate...yes...
                    //fix it later
                    if (arma.AR == null || arma.AR.Coefficients == null)
                    {
                    }

                    arma = ARMASimple2(estimationData, arSes, maSes);

                    var inSampleRes = Infer(arma, estimationData);

                    forecast = MarketModels.TimeSeries.Forecast(estimationData, inSampleRes, arma, horizon, confidence);

                    //when done out of sample, matches the realized and forecasted lengths to compute fit... 
                    //if no available data, no fit can be done...
                    forecasted = forecast.Forecast.Take(rlzd.Length).ToArray();

                    //allow only AR and MA coefficients to be changed
                    //model = new { AR = arma.AR, MA = arma.MA };
                    model = null;
                }
                else
                {
                    var arxma = JsonConvert.DeserializeObject<ARXMAModel>(MathModel);

                    var vars = exogenousVariablesJs.Properties()
                        .Where(p => p.Value.ToString() == "True")
                        .Select(p => p.Name)
                        .ToList();

                    var specialDaysSelected = vars.Remove("SpecialWeekDays");

                    var exData = vars
                        .Select(p =>
                            AppData.GetHistoricalSeries(_exVarsPre + p + "_Hourly_All.json")
                                .Where(x => x.DateTime < dt)
                                .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..
                                .Reverse().Take(estimationDataLength + horizon).Reverse().ToArray()
                                .ToList()
                        ).ToList().ColumnsToRectangularArray();

                    //append indicators for special data...
                    if (specialDaysSelected)
                    {
                        //Friday, Saturday and Sunday
                        var ivars = MathFunctions.IndicatorVariablesMatrix(estimationDataLength + horizon, 168, 24, new int[] { 0, 5, 6 });

                        //concatenate matrices to columns
                        if (exData.GetLength(1) > 0)
                            exData = MathFunctions.concat2D(exData, ivars, false);
                        else
                            exData = ivars;
                    }

                    var exDataEst = MathFunctions.firstRows2D(exData, estimationDataLength);

                    arxma = ARXMASimple2(estimationData, exDataEst, arSes, maSes);

                    var inSampleRes = arxma.Infer(estimationData, exDataEst);

                    forecast = arxma.Forecast(estimationData, inSampleRes, exData, horizon, confidence);

                    //when done out of sample, matches the realized and forecasted lengths to compute fit... 
                    //if no available data, no fit can be done...
                    forecasted = forecast.Forecast.Take(rlzd.Length).ToArray();

                    //allow only AR and MA coefficients to be changed
                    //model = new { AR = arma.AR, MA = arma.MA };
                    model = null;
                }
            }
            else
                throw new ArgumentException("Unsupported argument passed");

            var log = "";

            if (forecast.Forecast.Any(x => x > 500))
                log += "Some data is wrong...";

            if (forecast.Forecast.HasInvalidData() || forecast.Confidence.HasInvalidData())
                throw new Exception("Abnormal results generated");

            /* Post processing */
            //cap all values above a reasonable limit, e.g. 500
            CapSeries(forecast.Forecast, 500, -100);
            CapMultipleSeries(forecast.Confidence, 500, -100);

            FitStatistics eFit = null, eBFit = null, ePFit = null, fit = null, bfit = null, pfit = null;

            if (rlzd.Length > 0)
            {
                fit = ForecastFit(forecasted, rlzd);
                pfit = ForecastFit(
                    GetSubPeriodsFrom(forecasted, 24, DAY_PEAK_HOURS),
                    GetSubPeriodsFrom(rlzd, 24, DAY_PEAK_HOURS));
                bfit = ForecastFit(
                    GetSubPeriodsFrom(forecasted, 24, DAY_BASE_HOURS),
                    GetSubPeriodsFrom(rlzd, 24, DAY_BASE_HOURS));
            }

            if (estimationData != null && forecast.Backcast.Length > 0)
            {
                //model with estimation, that is: the model is different than naive
                eFit = ForecastFit(forecast.Backcast, estimationData);
                ePFit = ForecastFit(
                    GetSubPeriodsFrom(forecast.Backcast, 24, DAY_PEAK_HOURS),
                    GetSubPeriodsFrom(estimationData, 24, DAY_PEAK_HOURS));
                eBFit = ForecastFit(
                    GetSubPeriodsFrom(forecast.Backcast, 24, DAY_BASE_HOURS),
                    GetSubPeriodsFrom(estimationData, 24, DAY_BASE_HOURS));
            }
            var obj = new
            {
                Result = forecast,
                MathModel = model,
                DaysAhead = horizon / 24,
                //in sample fit for all hours
                EstimationFit = eFit,
                BaseEstimationFit = eBFit,
                PeakEstimationFit = ePFit,
                Fit = fit,
                BaseFit = bfit, //refine later...
                PeakFit = pfit,
                //additional remarks about the resulting data...
                Log = log
            };

            return Json(obj, JsonRequestBehavior.AllowGet);
        }
    }
}