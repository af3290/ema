using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using EMA.DomainModels;
using Utils;
using EMA.Misc;

namespace EMA.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            //tests here fow now...

            //var np = new NordicPrices();
            //var models = np.QuantitativeModels.ToList();
            //apparently we got only 156.. matlab has got 157... we missed one, which one...?
            //
            //AppData.GasSpotExcelToJson();

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult TimeSeriesTest()
        {
            return View();
        }
    }
}