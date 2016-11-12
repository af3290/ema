using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Utils.Model;
using static MarketModels.Types;

namespace EMA.Controllers
{
    //TODO: rename to electricity... since we all do electricity here...
    public class EnergyMarketsController : Controller
    {
        public ActionResult SpotCurvesAnalysis()
        {

            return View();
        }

        public ActionResult SpotPriceForecast()
        {
            //to be automatically detected from available data...
            var exVar = new string[] {
                "SpecialWeekDays",
                "Production", "Production-Prognosis",
                //"Consumption", "Consumption-Prognosis",
                //"Wind", "Hydro"
            };
            var exVarJs = JsonConvert.SerializeObject(exVar);
            ViewBag.ExogenousVariables = exVarJs;
            return View();
        }

        public ActionResult SpotPriceSimulations()
        {
            return View();
        }

        public ActionResult Aggregate()
        {
            return View();
        }

        public ActionResult ForwardMarketAnalysis()
        {
            return View();
        }

        public ActionResult PowerPurchaseAgreement()
        {
            return View();
        }
        
        /// <summary>
        /// Weighted average of contracts by their hourly lengths
        /// </summary>
        /// <param name="fcs"></param>
        /// <param name="priceSelector"></param>
        /// <returns></returns>
        decimal WA(List<ForwardContract> fcs, Func<ForwardContract, decimal> priceSelector)
        {
            return fcs.Sum(f => priceSelector(f) * f.Hours) / fcs.Sum (f => f.Hours) ;
        }

        public JsonResult ArbVals()
        {
            var dd = new Utils.NasdaqOMX.Downloader();
            var fc = dd.ForwardCurve(false);

            /* Find all overlapping contracts */
            var years = fc.Where(c => c.Resolution == Resolution.Yearly).ToList();
            var quarters = fc.Where(c => c.Resolution == Resolution.Quarterly).ToList();
            var months = fc.Where(c => c.Resolution == Resolution.Monthly).ToList();
            //Months in Quarters
            var mqs = quarters
                .Where(quarter => months.Count(m => quarter.Begin <= m.Begin && m.End <= quarter.End) == 3)
                .Select(quarter => new Tuple<ForwardContract, List<ForwardContract>>(quarter, months
                    .Where(month => quarter.Begin <= month.Begin && month.End <= quarter.End)
                    .ToList())
                ).ToList();
            //Quarters in Years
            var qys = years
                .Where(year => quarters.Count(quarter => quarter.Begin.Year == year.Begin.Year) == 4)
                .Select(year => new Tuple<ForwardContract, List<ForwardContract>>(year, quarters
                    .Where(quarter => quarter.Begin.Year == year.Begin.Year)
                    .ToList())
                ).ToList();
            var allPairs = new List<Tuple<ForwardContract, List<ForwardContract>>>();
            allPairs.AddRange(mqs);
            allPairs.AddRange(qys);

            /* Look for arbitrages */
            //Long
            var longs = allPairs.Select(p => new {
                Pair = p.Item1.ContractS + " <-> " + string.Join(", ", p.Item2.Select(x=>x.ContractS)),
                Direction = "Long",
                Leg1 = -p.Item1.Ask,
                Leg2 = + WA(p.Item2, f => f.Bid),
                PnL = -p.Item1.Ask + WA(p.Item2, f => f.Bid)
            }).ToList();
            //Short
            var shorts = allPairs.Select(p => new
            {
                Pair = p.Item1.ContractS + " <-> " + string.Join(", ", p.Item2.Select(x => x.ContractS)),
                Direction = "Short",
                Leg1 = +p.Item1.Bid,
                Leg2 = -WA(p.Item2, f => f.Ask),
                PnL = +p.Item1.Bid - WA(p.Item2, f => f.Ask)
            }).ToList();

            var allPositions = new List<object>();
            allPositions.AddRange(longs);
            allPositions.AddRange(shorts);

            return Json(allPositions, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ForwardArbitrages()
        {
            //check if available volumes!... 

            //prepare view... yeah...

            return View();
        }        
    }
}