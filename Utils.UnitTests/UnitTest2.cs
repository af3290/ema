using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.NordPoolSpot;
using MarketModels;
using static MarketModels.Types;

namespace Utils.UnitTests
{
    [TestClass]
    public class UnitTest2
    {
        private DateTime FromYear(int year) { return new DateTime(year, 1, 1); }
        //TODO: download files, convert to json, build day-ahead price forecasting! AWESOME POSSUM! really cool stuff...
        //then use dapper to make it into a simple QA model platform...

        [TestMethod]
        public void NordPoolSpotDownloadAllFiles()
        {
            var nd = new NordPoolSpot.DataDownloader();
            var path = "";

            /*
            path = nd.DownloadFile(DataItem.Production, Country.All, Resolution.Hourly, FromYear(2013));
            path = nd.DownloadFile(DataItem.Production, Country.All, Resolution.Hourly, FromYear(2014));
            path = nd.DownloadFile(DataItem.Production, Country.All, Resolution.Hourly, FromYear(2015));
            
            path = nd.DownloadFile(DataItem.Production_Prognosis, Resolution.Hourly, FromYear(2013));
            path = nd.DownloadFile(DataItem.Production_Prognosis, Resolution.Hourly, FromYear(2014));
            path = nd.DownloadFile(DataItem.Production_Prognosis, Resolution.Hourly, FromYear(2015));
            
            path = nd.DownloadFile(DataItem.Consumption, Country.All, Resolution.Hourly, FromYear(2013));
            path = nd.DownloadFile(DataItem.Consumption, Country.All, Resolution.Hourly, FromYear(2014));
            path = nd.DownloadFile(DataItem.Consumption, Country.All, Resolution.Hourly, FromYear(2015));

            //note that this one doesn't have regions, it's for ALL Countries
            path = nd.DownloadFile(DataItem.Consumption_Prognosis, Resolution.Hourly, FromYear(2013));
            path = nd.DownloadFile(DataItem.Consumption_Prognosis, Resolution.Hourly, FromYear(2014));
            path = nd.DownloadFile(DataItem.Consumption_Prognosis, Resolution.Hourly, FromYear(2015));
            */

            /*
            //this has JUST daily values for all countries... wind is most significant in DK => hence that's what matters..
            //it's too small for SE, so exclude it...
            path = nd.DownloadFile(DataItem.Wind_Power, Country.DK, Resolution.Hourly, FromYear(2013));
            path = nd.DownloadFile(DataItem.Wind_Power, Country.DK, Resolution.Hourly, FromYear(2014));
            path = nd.DownloadFile(DataItem.Wind_Power, Country.DK, Resolution.Hourly, FromYear(2015));

            path = nd.DownloadFile(DataItem.Hydro_Reservoir, Resolution.Weekly, FromYear(2013));
            path = nd.DownloadFile(DataItem.Hydro_Reservoir, Resolution.Weekly, FromYear(2014));
            path = nd.DownloadFile(DataItem.Hydro_Reservoir, Resolution.Weekly, FromYear(2015));
            */
            var x = Resolution.Hourly;

            /*
            path = nd.DownloadFile(DataItem.Elspot_Prices, Resolution.Hourly, FromYear(2013), Currency.EUR);
            path = nd.DownloadFile(DataItem.Elspot_Prices, Resolution.Hourly, FromYear(2014), Currency.EUR);
            path = nd.DownloadFile(DataItem.Elspot_Prices, Resolution.Hourly, FromYear(2015), Currency.EUR);
            */

            path = nd.DownloadFile(DataItem.Elspot_Capacities, Resolution.Hourly, FromYear(2013), Currency.EUR);
            path = nd.DownloadFile(DataItem.Elspot_Capacities, Resolution.Hourly, FromYear(2014), Currency.EUR);
            path = nd.DownloadFile(DataItem.Elspot_Capacities, Resolution.Hourly, FromYear(2015), Currency.EUR);
        }

        [TestMethod]
        public void ConvertExcelToJSON()
        {
            

            //in todo... do this for all files.. then transfer code from PricesController...
        }
    }
}
