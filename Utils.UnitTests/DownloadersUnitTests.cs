using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils.NordPoolSpot;
using MarketModels;
using static MarketModels.Types;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Utils.UnitTests
{
    [TestClass]
    public class DownloadersUnitTests
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

            path = nd.DownloadFile(DataItem.Elspot_Capacities, Resolution.Hourly, FromYear(2013));
            path = nd.DownloadFile(DataItem.Elspot_Capacities, Resolution.Hourly, FromYear(2014));
            path = nd.DownloadFile(DataItem.Elspot_Capacities, Resolution.Hourly, FromYear(2015));
        }

        [TestMethod]
        public void ConvertExcelToJSON()
        {
            

            //in todo... do this for all files.. then transfer code from PricesController...
        }

        [TestMethod]
        public void JPXHourlyPrices()
        {
            var ca = File.ReadAllLines(@"C:\Users\Andrei Firte\Google Drev\Resources\Quantitative Analytics\Data\JEPX\Spot\spot_2005-2015.csv");

            var values = new List<HistoricalPrice>();

            var sdt = new DateTime(2005, 4, 2);

            for (int i = 0; i < ca.Length; i++)
            {
                var vals = ca[i].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                var p = decimal.Parse(vals[5]);
                //half hourly data!!!
                values.Add(new HistoricalPrice() { DateTime = sdt.AddHours(i*0.5), Value = p });
            }


            var pjson = JsonConvert.SerializeObject(values);
            File.WriteAllText(@"D:\Projects\GitHub\ema\EMA\App_Data\JPX_Price_Hourly.json", pjson);
        }

        [TestMethod]
        public void WeronCAHourly()
        {
            var ca = File.ReadAllLines(@"C:\Users\Andrei Firte\Google Drev\Resources\Quantitative Analytics\Electricity\Modelling and Forecasting Electricity Loads and Prices\MFE\CA_hourlyX.dat");

            var values = new List<List<HistoricalPrice>>();

            var sdt = new DateTime(1998, 4, 1);

            var prices = new List<HistoricalPrice>();
            var loads = new List<HistoricalPrice>();
            var loadForecasts = new List<HistoricalPrice>();

            for (int i = 0; i < ca.Length; i++)
            {
                var vals = ca[i].Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                var p = decimal.Parse(vals[2]);
                var d = decimal.Parse(vals[3]);
                var df = decimal.Parse(vals[4]);
                prices.Add(new HistoricalPrice() { DateTime = sdt.AddHours(i), Value = p });
                loads.Add(new HistoricalPrice() { DateTime = sdt.AddHours(i), Value = d });
                loadForecasts.Add(new HistoricalPrice() { DateTime = sdt.AddHours(i), Value = df });
            }

            
            var pjson = JsonConvert.SerializeObject(prices);
            File.WriteAllText(@"D:\Projects\GitHub\ema\EMA\App_Data\CA\Price_Hourly.json", pjson);
            var ljson = JsonConvert.SerializeObject(loads);
            File.WriteAllText(@"D:\Projects\GitHub\ema\EMA\App_Data\CA\Consumption_Hourly_All.json", ljson);
            var lfjson = JsonConvert.SerializeObject(loadForecasts);
            File.WriteAllText(@"D:\Projects\GitHub\ema\EMA\App_Data\CA\Consumption-Prognosis_Hourly_All.json", lfjson);
            //in todo... do this for all files.. then transfer code from PricesController...
        }

        [TestMethod]
        public void WeatherUnderground()
        {
            string s = "9:00 AM";
            var dt = s.TryCastToTime();

            s = "11:21 AM";
            dt = s.TryCastToTime();

            s = "9:55 PM";
            dt = s.TryCastToTime();

            s = "12:29 PM";
            dt = s.TryCastToTime();

            s = "23:21";
            dt = s.TryCastToTime();

            var wu = new Utils.WeatherUnderground.Downloader();
            var now = wu.WeatherData("Tokyo", DateTime.Now);

            var from = new DateTime(2005, 4, 2);
            var to = new DateTime(2016, 1, 28);
            
        }
    }
}
