using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Utils.WeatherUnderground
{
    public class WeatherObservation
    {
        public DateTime DateTime { get; set; }
        //°C
        public double Temperature { get; set; }
        //°C
        public double DewPoint { get; set; }
        //%
        public double Humidity { get; set; }
        //hPa
        public double Pressure { get; set; }
        //NW...etc..
        public string WindDir { get; set; }
        //km/h
        public double WindSpeed { get; set; }

        public override string ToString()
        {
            //handle error
            WindDir = WindDir.Replace("\n", "").Replace("\r", "");
            return string.Join(", ", new object[] { DateTime, Temperature, DewPoint, Humidity, Pressure, WindDir, WindSpeed });
        }
    }

    public class Downloader
    {
        public const string FeedURL = "https://www.wunderground.com/history/airport/RJTT/{0}/{1}/{2}/DailyHistory.html?req_city=Tokyo&req_state=&req_statename=Japan&reqdb.zip=00000&reqdb.magic=4&reqdb.wmo=47671&MR=1";

        public List<WeatherObservation> WeatherData(string location, DateTime from, DateTime to)
        {
            List<WeatherObservation> all = new List<WeatherObservation>();
            var totalDays = (to - from).TotalDays;
            var lastPrc = 0.0;
            var avgT = 0.0;
            var beginning = Environment.TickCount;
            var n = 0;

            for (var i = from; i <= to; i = i.AddDays(1))
            {
                var start = Environment.TickCount;
                try {
                    all.AddRange(WeatherData(location, i));
                }catch(Exception ex)
                {
                    Console.Write("Page donwload failed");
                } 
                var end = Environment.TickCount;

                //if debuggnig enabled...
                var prc = (i - from).TotalDays / totalDays * 100.0;
                var t = (end - start) / 1000.0;
                var e = (end - beginning) / 1000.0;
                //rolling average download time
                avgT = (n * avgT + t) / (n + 1);
                //estimated remaining time
                var r = (1 - prc) * (prc - lastPrc) * avgT;
                lastPrc = prc;
                n++;

                Console.WriteLine("Done date {0}, {1:00.00} % complete, took {2} secs, elapsed {3}, remaining {4}", 
                    i, prc, t, e, r);
            }
            return all;
        }

        public List<WeatherObservation> WeatherData(string location, DateTime date)
        {
            string result;

            using (WebClient cc = new WebClient())
            {
                var url = string.Format(FeedURL, date.Year, date.Month, date.Day);
                result = cc.DownloadString(url);
            }

            HtmlDocument htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(result);

            var observations = htmlDoc.DocumentNode.SelectNodes("//table[@id='obsTable']/tbody/tr");

            var obs = observations.Select((x, i) =>
            {
                var time = x.ChildNodes[1].InnerText.TryCastToTime();
                var wo = new WeatherObservation()
                {                    
                    //TODO: fix, some of the parsing is WRONG, check files...
                    //there's an error with the days too! YEAH...
                    DateTime = date.Add(time),
                    Temperature = GetDoubleFromTD(x.ChildNodes[3]),
                    DewPoint = GetDoubleFromTD(x.ChildNodes[7]),
                    Humidity = x.ChildNodes[9].InnerText.Replace("%", "").TryCastToDouble(),
                    Pressure = GetDoubleFromTD(x.ChildNodes[11]),
                    WindDir = x.ChildNodes[15].InnerText,
                    WindSpeed = GetDoubleFromTD(x.ChildNodes[17])
                };
                return wo;
            }).ToList();
            
            return obs;
        }

        private double GetDoubleFromTD(HtmlNode td)
        {
            if (td.ChildNodes.Count < 2 || td.ChildNodes[1].ChildNodes.Count < 1)
                return double.NaN;
            
            return td.ChildNodes[1].ChildNodes[0].InnerText.TryCastToDouble();
        }
    }
}
