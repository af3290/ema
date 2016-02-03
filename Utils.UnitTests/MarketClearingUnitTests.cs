using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MarketModels;

namespace Utils.UnitTests
{
    [TestClass]
    public class MarketClearingUnitTests
    {
        [TestMethod]
        public void MarketClearingTest_SupplyFirstMiddle()
        {
            //notice the format:
            //demand in decreasing qs and ps values
            //supply in decreasing ps values
            //the eq price is present both curves: 37.5
            var d = new double[,] {
                { 150, 200, 200}, //quantities
                { 30, 12, 8 } }; //prices

            var s = new double[,] {
                { 120,50,200 },
                { 0,15,30} };

            var equilQ = MarketClearing.FindEquilibrium(s, d);
            
            Assert.AreEqual(equilQ.totalTradedQuantity, 150, 1e-2);
            Assert.AreEqual(equilQ.eqPrice, 15, 1e-2);
        }

        [TestMethod]
        public void MarketClearingTest_PierrePinson()
        {
            //the eq price is present both curves: 37.5
            var d = new double[,] { 
                { 250, 300, 120, 80, 40, 70, 60, 45, 30, 35, 25, 10 }, //quantities
                { 200, 110, 100, 90, 85, 75, 65, 40, 37.5, 30, 24, 15 } }; //prices

            var s = new double[,] {
                { 120,50,200,400,60,50,60,100,70,50,70,45,50,60,50 },
                { 0,0,15,30,32.5,34,36,37.5,39,40,60,70,100,150,200 } };

            var equilQ = MarketClearing.FindEquilibrium(s, d);
                        
            Assert.AreEqual(equilQ.totalTradedQuantity, 965, 1e-2);
            //in slides he finds 955, why? nevermind
            Assert.AreEqual(equilQ.eqPrice, 37.5, 1e-2);
        }

        [TestMethod]
        public void MarketClearingTest_NordpoolspotHour8()
        {
            var d = new double[,] {
                { 250, 300, 120, 80, 40, 70, 60, 45, 30, 35, 25, 10 },
                { 200, 110, 100, 90, 85, 75, 65, 40, 37.5, 30, 24, 15 } };

            var s = new double[,] {
                { 120,50,200,400,60,50,60,100,70,50,70,45,50,60,50 },
                { 0,0,15,30,32.5,34,36,37.5,39,40,60,70,100,150,200 } };
            
            var equilQ = MarketClearing.FindEquilibrium(s, d);

            Assert.AreEqual(equilQ.totalTradedQuantity, 47077, 1e-2);
            Assert.AreEqual(equilQ.eqPrice, 29.92, 1e-2);
        }

        [TestMethod]
        public void MarketClearingTest_NordpoolspotHour18()
        {

        }
    }
}
