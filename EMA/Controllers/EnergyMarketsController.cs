using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
                "Consumption", "Consumption-Prognosis",
                "Wind", "Hydro"};
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
        
    }
}