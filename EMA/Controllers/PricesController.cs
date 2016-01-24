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
        public JsonResult Forecast(DateTime? date, string forecastMethod, string forecastModel,
            string timeHorizon, double confidence, string MathModel, string exogenousVariables)
        {            
            var dt = date.HasValue? date.Value:DateTime.Today; 
            var ths = GetUnionCaseNames<Types.TimeHorizon>();

            var forecastHorizon = Array.IndexOf(ths, timeHorizon); //TODO: finish here... yeah...
            //TODO: do it later...
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

            var th = GetTimeHorizonValue(forecastHorizon);

            var rlzd = d.Where(x => x.DateTime >= dt)
                .Take(th)
                .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..                
                .ToArray();

            ForecastResult forecast;
            double[] forecasted;
            object model;

            //add naive with exogenous variables next...
            if (forecastMethod == "Naive")
            {
                var halfYear = (int)Math.Floor((dt - dt.AddMonths(-6)).TotalHours);

                var allses = new int[] { halfYear, 24 * 7 * 4, 24 * 7, 24 };

                //we need always seasonalities > forecast horizon
                var ses = allses.Where(s => s >= th).ToArray();

                forecast = Naive(data, ses, th, confidence);

                forecasted = forecast.Forecast.Take(rlzd.Length).ToArray();

                model = new object();
            }
            else if (forecastMethod == "HWT")
            {
                var last3Month = data.Reverse().Take(24 * 7 * 2).Reverse().ToArray();
                var hwparams = JsonConvert.DeserializeObject<HoltWinters.HoltWintersParams>(MathModel);

                //optimize if all seem to be wrong
                if (hwparams.alpha == 0 || hwparams.beta == 0 || hwparams.gamma == 0)
                    hwparams = HoltWinters.OptimizeTripleHWT(last3Month, 24, th);
                
                forecast = HoltWinters.TripleHWTWithPIs(last3Month, 24, th, hwparams, confidence);
               
                forecasted = forecast.Forecast.Take(rlzd.Length).ToArray();

                model = hwparams;
            }
            //else if (forecastMethod == "AR")
            //{
            //    var last3Month = data.Reverse().Take(24 * 7 * 4 * 3).Reverse().ToArray();

            //    var arma = ARMASimple2(last3Month, new int[] { 1, 2, 24 }, new int[] { 24 });
            //}
            else
                throw new ArgumentException("Unsupported argument passed");
                                    
            FitStatistics fit = null, bfit = null, pfit = null;

            if (rlzd.Length > 0) {
                fit = ForecastFit(forecasted, rlzd);
                pfit = ForecastFit(
                    GetSubPeriodsFrom(forecasted, 24, DAY_PEAK_HOURS), 
                    GetSubPeriodsFrom(rlzd, 24, DAY_PEAK_HOURS));
                bfit = ForecastFit(
                    GetSubPeriodsFrom(forecasted, 24, DAY_BASE_HOURS),
                    GetSubPeriodsFrom(rlzd, 24, DAY_BASE_HOURS));
            }

            var obj = new
            {                
                Result = forecast,
                MathModel = model,
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