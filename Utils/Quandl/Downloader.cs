using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.Quandl
{
    public class Downloader
    {
        private Quandl quandl;
        private Dictionary<string, string> settings;

        public Downloader()
        {
            quandl = new Quandl("eJkRsvbRyE3xK1NbqYVf");
            settings = new Dictionary<string, string>();
            settings.Add("type", "data");
            //settings.Add("trim_start", "2010-02-01");
            //settings.Add("trim_end", "2010-04-28");
            //settings.Add("transformation", "normalize");
            settings.Add("sort_order", "asc");
        }

        //for henry hub... yes...
        public TimeSeries GetGasSpot()
        {
            var dataset = "EIA/NG_RNGWHHD_D";
            var data = quandl.GetRawData(dataset, settings, "json");

            var ts = RawToTimeSeries(data, seriesItem => new HistoricalPrice
            {
                DateTime = seriesItem[0].ToString().TryCastToDateTime(),
                Value = seriesItem[1].ToString().TryCastToDecimal(),
            });

            return ts;
        }

        public List<TimeSeries> GetGasFutures()
        {
            var tss = new List<TimeSeries>();
                        
            //var alpha = "FGH";
            var alpha = "FGHJKMNQUVXZ";

            //var year = 2004;
            var year = DateTime.Now.Year;

            for (int i = 2004; i <= year; i++)
            {
                for (int j = 0; j < alpha.Length; j++)
                {
                    var dataset = string.Format("OFDP/FUTURE_NG{0}{1}", alpha[j], i);

                    var data = quandl.GetRawData(dataset, settings, "json");

                    var ts = RawToTimeSeries(data, seriesItem => new HistoricalPrice
                    {
                        DateTime = seriesItem[0].ToString().TryCastToDateTime(),
                        Value = seriesItem[4].ToString().TryCastToDecimal(),
                    });

                    tss.Add(ts);
                }
            }

            
            return tss;
        }

        private TimeSeries RawToTimeSeries(string data, Func<JToken, HistoricalPrice> dataFunc)
        {
            var obj = JObject.Parse(data);

            var series = obj["data"].ToArray();

            var ts = new TimeSeries()
            {
                Name = obj["urlize_name"].ToString(),
                Description = obj["description"].ToString()
            };

            for (int k = 0; k < series.Length; k++)
            {
                var seriesItem = series[k];
                ts.Prices.Add(dataFunc(seriesItem));
            }

            return ts;
        }
    }
}
