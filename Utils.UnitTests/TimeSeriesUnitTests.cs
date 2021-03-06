﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MarketModels;
using static MarketModels.Tests.TimeSeriesTests;
using MarketModels.Tests;

namespace Utils.UnitTests
{
    [TestClass]
    public class TimeSeriesUnitTests
    {
        [TestMethod]
        public void HWTTest1()
        {
            double[] y = { 362, 385, 432, 341, 382, 409, 498, 387, 473, 513, 582, 474,
                544, 582, 681, 557, 628, 707, 773, 592, 627, 725, 854, 661 };
            int period = 4;
            int m = 7;

            double alpha = 0.5;
            double beta = 0.4;
            double gamma = 0.6;

            double[] prediction = HoltWinters.TripleHWT(y, period, m, alpha, beta, gamma);

            var tst = string.Join(" ", y.Select(x => x.ToString()));
            var tstpred = string.Join(" ", prediction.Select(x => x.ToString()));

            // These are the expected results
            double[] expected = new double[] { };

            //Assert.AreEqual("Forecast does not match", expected,
            //        prediction, 0.0000000000001);
        }

        [TestMethod]
        public void HWTTest2WeekSystemPrice()
        {
            double[] y = { 31.0500, 30.4700, 28.9200, 27.8800, 26.9600, 27.8400, 28.7900, 28.6300, 28.4400, 28.3000, 30.6500, 31.5500, 32.1600, 32.4500, 32.6300, 33.6500, 34.9000, 36.2200, 36.6500, 36.3700, 35.4900, 34.4100, 34.6600, 32.5500, 33.1500, 32.6600, 31.8300, 31.4700, 32.5600, 34.3600, 36.2800, 38.3900, 39.0900, 38.3300, 38.4200, 38.2500, 37.9600, 37.8900, 37.8800, 38.7800, 39.8300, 39.9100, 39.3200, 38.4900, 37.4600, 36.9400, 36.3700, 34.5900, 33.1100, 32.2200, 31.4600, 31.6700, 32.0500, 33.6700, 34.9300, 35.8200, 36.3800, 36.5200, 36.7100, 36.6000, 36.5100, 36.4000, 36.4200, 36.5800, 36.9400, 36.9400, 36.8100, 36.4300, 35.9100, 35.4500, 34.7700, 32.0900, 31.7100, 30.8600, 30.2100, 30.3600, 30.8900, 32.2100, 34.3300, 35.9800, 36.7200, 36.6800, 36.8000, 36.6400, 36.4400, 36.3100, 35.8300, 36.1200, 36.8000, 37.2900, 37.1500, 36.4700, 36.0300, 35.4300, 35.1700, 32.9500, 33.6900, 32.7100, 32.0900, 32.1700, 32.7300, 33.7700, 34.0300, 33.9800, 34.4500, 36.1200, 36.6900, 36.7400, 36.5200, 36.1200, 36.1400, 36.7800, 37.7100, 38.3700, 37.9900, 36.9400, 36.2400, 35.8900, 35.7000, 35.4400, 34.8700, 33.0500, 32.2500, 32.4100, 32.4500, 32.8000, 33.2700, 32.9700, 33.6500, 34.2500, 35.0100, 35.9400, 35.7900, 35.0200, 34.8500, 35.1800, 35.9000, 37.0900, 37.6500, 37.3900, 36.9900, 36.1000, 36.0000, 34.9300, 34.9700, 33.7100, 33.1300, 33.2300, 34.1000, 35.6800, 38.5100, 40.6600, 42.4800, 42.0800, 41.9700, 41.3700, 40.3000, 39.9600, 40.1700, 41.6000, 42.3700, 41.9300, 39.9700, 38.9000, 37.9200, 37.4700, 36.4900, 35.5500 };
            int period = 24;
            int m = 3 * 24;

            double alpha = 0.5;
            double beta = 0.4;
            double gamma = 0.6;
            
            double[] prediction = HoltWinters.TripleHWT(y, period, m, alpha, beta, gamma);

            var tst = string.Join(" ", y.Select(x => x.ToString()));
            var tstpred = string.Join(" ", prediction.Select(x => x.ToString()));

            //Assert.AreEqual("Forecast does not match", expected,
            //        prediction, 0.0000000000001);
        }

        [TestMethod]
        public void HWTTest2WeekSystemPriceOptim()
        {
            double[] y = { 31.0500, 30.4700, 28.9200, 27.8800, 26.9600, 27.8400, 28.7900, 28.6300, 28.4400, 28.3000, 30.6500, 31.5500, 32.1600, 32.4500, 32.6300, 33.6500, 34.9000, 36.2200, 36.6500, 36.3700, 35.4900, 34.4100, 34.6600, 32.5500, 33.1500, 32.6600, 31.8300, 31.4700, 32.5600, 34.3600, 36.2800, 38.3900, 39.0900, 38.3300, 38.4200, 38.2500, 37.9600, 37.8900, 37.8800, 38.7800, 39.8300, 39.9100, 39.3200, 38.4900, 37.4600, 36.9400, 36.3700, 34.5900, 33.1100, 32.2200, 31.4600, 31.6700, 32.0500, 33.6700, 34.9300, 35.8200, 36.3800, 36.5200, 36.7100, 36.6000, 36.5100, 36.4000, 36.4200, 36.5800, 36.9400, 36.9400, 36.8100, 36.4300, 35.9100, 35.4500, 34.7700, 32.0900, 31.7100, 30.8600, 30.2100, 30.3600, 30.8900, 32.2100, 34.3300, 35.9800, 36.7200, 36.6800, 36.8000, 36.6400, 36.4400, 36.3100, 35.8300, 36.1200, 36.8000, 37.2900, 37.1500, 36.4700, 36.0300, 35.4300, 35.1700, 32.9500, 33.6900, 32.7100, 32.0900, 32.1700, 32.7300, 33.7700, 34.0300, 33.9800, 34.4500, 36.1200, 36.6900, 36.7400, 36.5200, 36.1200, 36.1400, 36.7800, 37.7100, 38.3700, 37.9900, 36.9400, 36.2400, 35.8900, 35.7000, 35.4400, 34.8700, 33.0500, 32.2500, 32.4100, 32.4500, 32.8000, 33.2700, 32.9700, 33.6500, 34.2500, 35.0100, 35.9400, 35.7900, 35.0200, 34.8500, 35.1800, 35.9000, 37.0900, 37.6500, 37.3900, 36.9900, 36.1000, 36.0000, 34.9300, 34.9700, 33.7100, 33.1300, 33.2300, 34.1000, 35.6800, 38.5100, 40.6600, 42.4800, 42.0800, 41.9700, 41.3700, 40.3000, 39.9600, 40.1700, 41.6000, 42.3700, 41.9300, 39.9700, 38.9000, 37.9200, 37.4700, 36.4900, 35.5500 };
            int period = 24;
            int m = 3 * 24;

            double alpha = 0.5;
            double beta = 0.4;
            double gamma = 0.6;

            var abg = HoltWinters.OptimizeTripleHWT(y, period, m);

            double[] prediction = HoltWinters.TripleHWT(y, period, m, abg.alpha, abg.beta, abg.gamma);

            var tst = string.Join(" ", y.Select(x => x.ToString()));
            var tstpred = string.Join(" ", prediction.Select(x => x.ToString()));
            
            //Assert.AreEqual("Forecast does not match", expected,
            //        prediction, 0.0000000000001);
        }

        [TestMethod]
        public void ARMA()
        {
            //var mrxx = new EstimationTests.EstimationUnitTests();
            //mrxx.MLE_Test1();

            var ts = new MarketModels.Tests.TimeSeriesTests.TimeSeriesUnitTests();
            ts.FilterTest1D1();
            ts.ARMASimulateModel();
            //ts.ARMAInferResidualsOfSpecifiedModel();
            //ts.TestFloatingPointErrorsVsMatlab();
            //ts.FilterTest2();
            var sp = new StochasticProcessesTests.StochasticProcessesUnitTests();
            //sp.OUEst();
            
            //var sts = new SimulationsTests.TimeSeriesUnitTests();
            
            //sts.PPATest();

            //ts.ARMATest1();
            //ts.OptimTest1();
            //ts.ARMATestLongerLags();
            //ts.ARMAForecastSpecifiedModel();
            ts.EstimateSpikesTest();
            ts.MovingSDsLag1();
            ts.MovingSDsLag2();
            //ts.ARMAXTest1_IndicatorVariables();
        }
    }

}
