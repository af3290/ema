using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Utils.UnitTests
{
    [TestClass]
    public class PricesCurvesModelUnitTest
    {
        [TestMethod]
        public void CurvesAreOrderedProperly()
        {
            //IF we choose to have it maintained them ordered...
        }

        [TestMethod]
        public void EquilibriumFallsOnCommonPoint()
        {
            var pcm = new PriceCurvesModel()
            {
                DemandCurve = new List<MarketPoint>()
                {
                    new MarketPoint() {Volume = 1, Price = 100},
                    new MarketPoint() {Volume = 2, Price = 70},
                    new MarketPoint() {Volume = 3, Price = 50},
                    new MarketPoint() {Volume = 4, Price = 30},
                },
                SupplyCurve = new List<MarketPoint>()
                {
                    new MarketPoint() {Volume = 1.5m, Price = -2},
                    new MarketPoint() {Volume = 2.3m, Price = 30},
                    new MarketPoint() {Volume = 3, Price = 50},
                    new MarketPoint() {Volume = 4.5m, Price = 80},
                }
            };

            Assert.AreEqual((double) pcm.Equilibrium.Volume, 3, 1e-5);
            Assert.AreEqual((double) pcm.Equilibrium.Price, 50, 1e-5);
        }

        [TestMethod]
        public void EquilibriumFallsBetweenPoints()
        {
            var pcm = new PriceCurvesModel()
            {
                DemandCurve = new List<MarketPoint>()
                {
                    new MarketPoint() {Volume = 1, Price = 100},
                    new MarketPoint() {Volume = 2, Price = 70},
                    new MarketPoint() {Volume = 3, Price = 50},
                    new MarketPoint() {Volume = 4, Price = 30},
                },
                SupplyCurve = new List<MarketPoint>()
                {
                    new MarketPoint() {Volume = 1.5m, Price = -2},
                    new MarketPoint() {Volume = 2.3m, Price = 20},
                    new MarketPoint() {Volume = 3.2m, Price = 30},
                    new MarketPoint() {Volume = 4.5m, Price = 50},
                }
            };
            
            Assert.AreEqual((double)pcm.Equilibrium.Volume, 3.5, 1);
            Assert.AreEqual((double)pcm.Equilibrium.Price, 45, 10);
        }

        [TestMethod]
        public void StatnettTicks()
        {
            //1446806460000 <--- stattnet
            //14468103784365439 <--- .NET

            var tx = new DateTime(2015, 11, 6, 0, 0, 0).Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            //14467680000000000 <-- today's ticks

            //what do those mean?
            //1446806700000
            //1446804720000
            //1446811354340
            //1446811380315
            var d1 = DateTime.Parse("01/01/1970 00:00:00").AddTicks(1446806700000 * 10000);
            var d2 = DateTime.Parse("01/01/1970 00:00:00").AddTicks(1446804720000 * 10000);

            var d3 = DateTime.Parse("01/01/1970 00:00:00").AddTicks(1446811380315 * 10000);
        }
    }
}
