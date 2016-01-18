using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Utils;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using static MarketModels.Types;
using EMA.Misc;
using MarketModels;
using Newtonsoft.Json.Linq;

namespace EMA.Controllers
{
    public class PricesController : Controller
    {        
        //TODO: add uniqueStructure only for non overlapping short contracts to take priority
        public JsonResult ForwardCurve(string region, bool nonOverlapping)
        {
            var dd = new Utils.NasdaqOMX.Downloader();
            var fc = dd.ForwardCurve(nonOverlapping);
            return Json(fc, JsonRequestBehavior.AllowGet);
        }
        
        public JsonResult HistoricalSystemPrice(bool refresh, int resolution)
        {
            if (resolution == 5)//because of bullshit F# types... dummy for now... fix later
            {
                var d = AppData.GetHistoricalSeries("SystemPrice_Hourly_EUR.json");
                return Json(d, JsonRequestBehavior.AllowGet);
            }

            //TODO: persist...
            var dailyHistoricalPrices = AppData.GetHistoricalSeries("SystemPrice_Daily_EUR.json");

            return Json(dailyHistoricalPrices, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GasFutures(DateTime? date,
            DateTime? afterDate, DateTime? beforeDate, 
            DateTime? afterMaturity, DateTime? beforeMaturity)
        {
            var x = AppData.GasForwards()
                .OrderBy(ts=>ts.Maturity)
                .ToList();

            if(afterMaturity.HasValue)
            {

            }

            if (beforeMaturity.HasValue)
            {

            }

            if (beforeMaturity.HasValue && afterMaturity.HasValue)
            {
                //typically the next month after last trading datetime is the maturity
                //x = x.Where(t => afterMaturity <= t.Maturity && t.Maturity <= beforeMaturity).ToList();
            }

            if(date.HasValue)
            {
                //too inefficient?
                //all futures that have data at the available at datetime, all futures which represent maturities to this spot datetime
                x = x.Where(t => t.Prices.First().DateTime <= date && date <= t.Prices.Last().DateTime).ToList();

                //but truncate to show data only until now... makes sense?
                //x = x.Select( t =>)
            }

            return Json(x, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LiveNordicProductionConsumption()
        {
            /* Convert to norway's time zone */
            var date = DateTime.UtcNow; //.AddHours(-1)

            /* Aproximate to latest minute */
            date = date.AddSeconds(-DateTime.Now.Second);
            var timestamp = date.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            timestamp = timestamp/10000;

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
        public JsonResult Forecast(DateTime? date, string forecastMethod, 
            string timeHorizon, double confidence, string exogenousVariables)
        {            
            var dt = date.HasValue? date.Value:DateTime.Today; 
            var ths = Types.GetUnionCaseNames<Types.TimeHorizon>();

            var forecastHorizon = Array.IndexOf(ths, timeHorizon); //TODO: finish here... yeah...

            if (forecastHorizon < 0)
                throw new ArgumentException("Wrong argument");

            var exogenousVariablesJs = JObject.Parse(exogenousVariables); //alternative...?
            var exVars = exogenousVariablesJs.Properties()
                .Where(p => p.Value.ToString() == "True")
                .Select(p => AppData.GetHistoricalSeries(p.Name + "_Hourly_All.json"))
                .ToList();

            //naive hardcoded for now...
            var d = AppData.GetHistoricalSeries("SystemPrice_Hourly_EUR.json");
            
            var data = d.Where(x => x.DateTime < dt)
                .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..
                .ToArray();
            
            var halfYear = (int)Math.Floor((dt - dt.AddMonths(-6)).TotalHours);
            
            var allses = new int[] { halfYear, 24 * 7*4, 24 * 7, 24 };
            var th = GetTimeHorizonValue(forecastHorizon);
            //we need always seasonalities > forecast horizon
            var ses = allses.Where(s => s >= th).ToArray();

            //add naive with exogenous variables next...
            if(forecastMethod == "Naive")
            {

            } else if (forecastMethod == "HWT")
            {

            }
            var forecast = MarketModels.Forecast.Naive(data, ses, th, 0.95);

            var last3Month = data.Reverse().Take(24 * 7 * 4 * 3).Reverse().ToArray();
            //var hwparams = HoltWinters.OptimizeTripleHWT(last3Month, 24, th);
            //hwparams[0], hwparams[1], hwparams[2]
            //var forecastVals = HoltWinters.TripleHWT(last3Month, 24, th, 0.5, 0.4, 0.6);
            //remember it gives all back...
            //forecastVals = forecastVals.Reverse().Take(th).Reverse().ToArray();
            //confidences are empty... because HWT doesn't output them... yet...
            //var forecast = new Forecast.ForecastResult(forecastVals, new double[,] { });

            var rlzd = d.Where(x => x.DateTime >= dt)
                .Take(th)
                .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..                
                .ToArray();

            //var forecasted = forecast.Forecast.Take(rlzd.Length).ToArray();
            var forecasted = forecast.Forecast.Reverse().Take(rlzd.Length).Reverse().ToArray();

            Forecast.FitStatistics fit = null, bfit = null, pfit = null;

            if (rlzd.Length > 0) {
                fit = MarketModels.Forecast.ForecastFit(forecasted, rlzd);
                pfit = MarketModels.Forecast.ForecastFit(
                    GetSubPeriodsFrom(forecasted, 24, DAY_PEAK_HOURS), 
                    GetSubPeriodsFrom(rlzd, 24, DAY_PEAK_HOURS));
                bfit = MarketModels.Forecast.ForecastFit(
                    GetSubPeriodsFrom(forecasted, 24, DAY_BASE_HOURS),
                    GetSubPeriodsFrom(rlzd, 24, DAY_BASE_HOURS));
            }

            var obj = new
            {                
                Result = forecast,
                DaysAhead = th / 24,
                Fit = fit,
                BaseFit = bfit, //refine later...
                PeakFit = pfit
            };
        
            return Json(obj, JsonRequestBehavior.AllowGet);        
        }

        public ActionResult ForecastTest()
        {
            return View();
        }
    }
}