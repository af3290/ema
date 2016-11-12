using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EMA.Controllers;
using EMA.Misc;

namespace NordicPricesAPI.UnitTests
{
    [TestClass]
    public class PricesControllerUnitTests
    {
        [TestMethod]
        public void SpikesTest()
        {
            var xx = new PricesController();
            var res = xx.EstimateSpikes("", "SimilarDay", 1.4);

            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void Simulations()
        {
            var xx = new SimulationsController();
            //find a way to give dummy data... not needed yet...
            var fcs = xx.ForwardCurveSimulations("Natural-Gas-Futures-NYMEX", new DateTime(2010, 1, 1), 12, "PCA", "");

            Assert.IsNotNull(fcs);
        }

        [TestMethod]
        public void HistoricalSystemPriceTest()
        {
            var x = new PricesController();
            //var z = x.SpotMarketCurvesObject(null, 1);
            //x.HistoricalSystemPrice(true, null);
            AppData.ExcelToJson();
        }
    }
}
