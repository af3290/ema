using EMA.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static MarketModels.Electricity;

namespace EMA.Controllers
{
    public class ValuationsController : Controller
    {
        [HttpPost]
        public JsonResult PowerPurchaseAgreement(int horizon, double retailPrice, 
            double capacity, double margin, double confidence)
        {
            var daily = AppData.GetHistoricalSeriesDaily("SystemPrice_Hourly_EUR.json");
            var data = daily
                    .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..
                    .ToArray();

            var ppa = new PowerPurchasingAgreement(margin, 0.025, retailPrice);

            ppa.Evaluate(data, horizon, confidence);

            var json = new
            {
                Simulations = ppa.SpotPrices.Take(20),
                Results = new {
                    NbSims = ppa.SpotPrices.Length,
                    Value = ppa.Value
                },
                Histogram = ppa.Histogram,
                MathModel = ppa.ModelParameters
            };

            return Json(json);
        }
    }
}