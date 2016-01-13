using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EMA.Controllers
{
    public class RiskManagementController : Controller
    {
        // GET: RiskManagement
        public ActionResult PlantPortfolio()
        {
            return View();
        }
    }
}