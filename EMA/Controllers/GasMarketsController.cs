using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EMA.Controllers
{
    public class GasMarketsController : Controller
    {
        // GET: GasMarket
        public ActionResult StorageValuation()
        {
            return View();
        }

        public ActionResult ForwardCurves()
        {
            return View();
        }
    }
}