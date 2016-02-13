using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Utils.NordPoolSpot;
using static MarketModels.Types;
using Utils;
using Newtonsoft.Json;
using Utils.Quandl;

namespace EMA.Misc
{
    public static class AppData
    {
        public static string GetAppDataServerPath()
        {
            return HttpContext.Current.Server.MapPath("~/App_Data/"); ;
        }
        public static string GetAppDataServerPathWith(string file)
        {
            return GetAppDataServerPath() + file; //Path.Combine(GetAppDataServerPath(), file)
        }
        private static DateTime FromYear(int year) { return new DateTime(year, 1, 1); }
        public static List<PriceCurvesModel> GetNordpoolMarketCurves(DateTime dateTime)
        {
            var file = string.Format(@"mcp_data_report_{0}-00_00_00",
                dateTime.ToString("dd-MM-yyyy"));

            string filePath = GetAppDataServerPathWith(@"\NordpoolSpot\" + file);

            var dd = new DataDownloader();
            var er = new Utils.ExcelReader();
            Stream fs;
            List<PriceCurvesModel> priceCurvesModels;

            /* If it doesn't exist, download it */
            if (!File.Exists(filePath + ".json"))
            {
                var data = dd.DownloadFile(file + ".xls");
                fs = new MemoryStream(data);
                priceCurvesModels = er.ReadNordPoolSpotPriceCurves(fs);
                fs.Dispose();
                Commons.SaveFileAsJson(filePath + ".json", priceCurvesModels);
            }
            else
            {
                var json = File.ReadAllText(filePath + ".json");
                priceCurvesModels = JsonConvert.DeserializeObject<List<PriceCurvesModel>>(json);
            }

            return priceCurvesModels;
        }
        public static List<HistoricalPrice> GetHistoricalSeries(string series)
        {
            var seriesPath = GetAppDataServerPath() + series;

            if (!File.Exists(seriesPath))
                return null;

            System.IO.FileStream f = System.IO.File.Open(seriesPath, FileMode.Open);
            var fs = new StreamReader(f);
            var json = fs.ReadToEnd();
            fs.Close();
            fs.Dispose();
            f.Close();
            f.Dispose();

            var d = JsonConvert.DeserializeObject<List<HistoricalPrice>>(json);

            //preprocessing... put it somewhere else
            d = d.Select((s, i) =>
            {
                decimal? v;
                if (s.Value.HasValue)
                    v = s.Value;
                else
                    v = d[i - 24].Value;
                return new HistoricalPrice() { DateTime = s.DateTime, Value = v };
            }).ToList();

            var nulls = d.Where(x => !x.Value.HasValue).ToList();

            return d;
        }

        public static List<double> GetConsumption()
        {
            return GetHistoricalSeriesDaily("Consumption_Hourly_All.json")
                                .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..
                                .ToList();
        }

        public static List<double> GetSystemPrice()
        {
            return GetHistoricalSeriesDaily("SystemPrice_Hourly_EUR.json")
                                .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..
                                .ToList();
        }

        public static List<double> GetHistoricalSeriesDailyValues(string series)
        {
            return GetHistoricalSeriesDaily(series)
                        .Select(x => x.Value != null ? (double)x.Value : 0) //0 for now, when interpolation is done... etc..
                        .ToList();
        }

        public static List<HistoricalPrice> GetHistoricalSeriesDaily(string series)
        {
            var d = GetHistoricalSeries(series);
            var sdt = d.First().DateTime;
            var days = d.Count / 24;
            var dailyHistoricalPrices = new List<HistoricalPrice>();
            for (int i = 0; i < days; i++)
            {
                var dayData = d.Skip(i * 24).Take(24);
                dailyHistoricalPrices.Add(new HistoricalPrice() { DateTime = sdt.AddDays(i), Value = (decimal)dayData.Select(x => x.Value).Average() });
            }
            return dailyHistoricalPrices;
        }

        public static List<TimeSeries> GasForwards()
        {
            var files = Directory.EnumerateFiles(GetAppDataServerPath() + @"\Gas\");
            var futures = files.Where(f => f.Contains("Futures")).ToList();

            var tss = new List<TimeSeries>();

            foreach (var item in futures)
            {
                var json = File.ReadAllText(item);
                //System.IO.FileStream f = System.IO.File.Open(item, FileMode.Open);
                //var fs = new StreamReader(f);
                //var json = fs.ReadToEnd();
                //fs.Close();
                //fs.Dispose();
                //f.Close();
                //f.Dispose();

                var d = JsonConvert.DeserializeObject<TimeSeries>(json);
                tss.Add(d);
            }

            return tss;
        }
        public static void GasSpotExcelToJson()
        {
            var d = new Downloader();
            var x = d.GetGasSpot();
            Commons.SaveFileAsJson(GetAppDataServerPath() + "/Gas/" + x.Name + ".json", x);
        }
        public static void GasExcelToJson()
        {
            var d = new Downloader();
            var x = d.GetGasFutures();

            foreach (var item in x)
            {
                Commons.SaveFileAsJson(GetAppDataServerPath() + "/Gas/" + item.Name + ".json", item);
            }
        }

        //TODO: find the proper place for this stuff
        public static void ExcelToJson()
        {
            //dependent
            //ExcelToJsonSeries(DataItem.Elspot_Prices);

            //predictors, 5 variables should be enough, for a simple naive forecast... yes...
            //ExcelToJsonSeriesPredictors1(DataItem.Production);
            //ExcelToJsonSeriesPredictors1(DataItem.Consumption);
            //ExcelToJsonSeriesPredictors2(DataItem.Production_Prognosis);
            //ExcelToJsonSeriesPredictors2(DataItem.Consumption_Prognosis);
            ExcelToJsonSeriesPredictors2(DataItem.Elspot_Capacities); //IN TODO..
            ////ExcelToJsonSeriesPredictors3(DataItem.Hydro_Reservoir);
            //ExcelToJsonSeriesPredictors3(DataItem.Wind_Power);
            ////ExcelToJsonSeries(DataItem.Wind_Power_Prognosis);
        }

        public static void ExcelToJsonSeries(DataItem di)
        {
            Func<int, string, string> downloadMethod = (int p1, string p2) => p2.Substring(p1);

            var er = new Utils.ExcelReader();
            var nd = new DataDownloader();
            var p = nd.BuildFileName(DataItem.Elspot_Prices, Resolution.Hourly, FromYear(2013), Currency.EUR);
            var hd = er.ReadNordPoolSpotHistoricalPricesInterop(DataDownloader.SavePath + p, 3);

            p = nd.BuildFileName(DataItem.Elspot_Prices, Resolution.Hourly, FromYear(2014), Currency.EUR);
            var hd1 = er.ReadNordPoolSpotHistoricalPricesInterop(DataDownloader.SavePath + p, 3);
            hd.AddRange(hd1);

            p = nd.BuildFileName(DataItem.Elspot_Prices, Resolution.Hourly, FromYear(2015), Currency.EUR);
            var hd2 = er.ReadNordPoolSpotHistoricalPricesInterop(DataDownloader.SavePath + p, 3);
            hd.AddRange(hd2);

            // do here some preprocessing?... interpolate missing and ourliers... but not spikes
            var negvs = hd.Where(x => x.Value < 0).ToList();
            var nulls = hd.Where(x => !x.Value.HasValue).ToList();

            var name = nd.BuildFileName(DataItem.Elspot_Prices, Resolution.Hourly, Currency.EUR);
            nd.SaveFileAsJson(GetAppDataServerPath() + name + ".json", hd);
        }

        //only for consumption and production...
        public static void ExcelToJsonSeriesPredictors1(DataItem di)
        {
            var er = new Utils.ExcelReader();

            Func<string, List<HistoricalPrice>> excelRead = (string path) =>
            {

                var hdx1 = er.ReadNordPoolSpotHistoricalPricesInterop(DataDownloader.SavePath + path, 3);
                var hdx2 = er.ReadNordPoolSpotHistoricalPricesInterop(DataDownloader.SavePath + path, 4);
                var hdx3 = er.ReadNordPoolSpotHistoricalPricesInterop(DataDownloader.SavePath + path, 5);
                var hdx4 = er.ReadNordPoolSpotHistoricalPricesInterop(DataDownloader.SavePath + path, 6);

                //needs to be done, since the sum is not already consistently calculated
                for (int i = 0; i < hdx1.Count; i++)
                {
                    hdx1[i].Value = hdx2[i].Value + hdx3[i].Value + hdx4[i].Value;
                }

                return hdx1;
            };

            var nd = new DataDownloader();

            Func<int, List<HistoricalPrice>> readYear = (int year) =>
            {
                var p = nd.BuildFileName(di, Country.All, Resolution.Hourly, FromYear(year));
                return excelRead(p);
            };

            var hd = readYear(2013);
            hd.AddRange(readYear(2014));
            hd.AddRange(readYear(2015));

            // do here some preprocessing?... interpolate missing and ourliers... but not spikes
            var negvs = hd.Where(x => x.Value < 0).ToList();
            var nulls = hd.Where(x => !x.Value.HasValue).ToList();

            var name = nd.BuildFileName(di, Country.All, Resolution.Hourly);
            nd.SaveFileAsJson(GetAppDataServerPath() + name + ".json", hd);
        }

        public static void ExcelToJsonSeriesPredictors2(DataItem di)
        {
            var er = new Utils.ExcelReader();

            Func<string, int, int, List<HistoricalPrice>> excelRead = (string path, int fromColumn, int toColumn) =>
            {
                var cols = new List<List<HistoricalPrice>>();
                for (int i = fromColumn; i <= toColumn; i++)
                {
                    cols.Add(er.ReadNordPoolSpotHistoricalPricesInterop(DataDownloader.SavePath + path, i));
                }

                //needs to be done, since the sum is not already consistently calculated
                //assumed same data lengths, ofc
                for (int i = 0; i < cols[0].Count; i++)
                {
                    decimal? val = 0m;

                    for (int j = 1; j < cols.Count; j++)
                    {
                        val += cols[j][i].Value;
                    }

                    cols[0][i].Value += val;
                }

                return cols[0];
            };

            var nd = new DataDownloader();

            Func<int, List<HistoricalPrice>> readYear = (int year) =>
            {
                var p = nd.BuildFileName(di, Resolution.Hourly, FromYear(year));
                if (di == DataItem.Consumption_Prognosis)
                    return excelRead(p, 3, 7);
                else
                    return excelRead(p, 3, 14);
            };

            var hd = readYear(2013);
            hd.AddRange(readYear(2014));
            hd.AddRange(readYear(2015));

            // do here some preprocessing?... interpolate missing and ourliers... but not spikes
            var negvs = hd.Where(x => x.Value < 0).ToList();
            var nulls = hd.Where(x => !x.Value.HasValue).ToList();

            var name = nd.BuildFileName(di, Country.All, Resolution.Hourly);
            nd.SaveFileAsJson(GetAppDataServerPath() + name + ".json", hd);
        }

        public static void ExcelToJsonSeriesPredictors3(DataItem di)
        {
            var er = new Utils.ExcelReader();

            Func<string, int, int, List<HistoricalPrice>> excelRead = (string path, int fromColumn, int toColumn) =>
            {
                var cols = new List<List<HistoricalPrice>>();
                for (int i = fromColumn; i <= toColumn; i++)
                {
                    cols.Add(er.ReadNordPoolSpotHistoricalPricesInterop(DataDownloader.SavePath + path, i));
                }

                //needs to be done, since the sum is not already consistently calculated
                //assumed same data lengths, ofc
                for (int i = 0; i < cols[0].Count; i++)
                {
                    decimal? val = 0m;

                    for (int j = 1; j < cols.Count; j++)
                    {
                        val += cols[j][i].Value;
                    }

                    cols[0][i].Value += val;
                }

                return cols[0];
            };

            var nd = new DataDownloader();

            Func<int, List<HistoricalPrice>> readYear = (int year) =>
            {
                var p = nd.BuildFileName(di, Resolution.Hourly, FromYear(year));
                if (di == DataItem.Hydro_Reservoir)
                    return excelRead(p, 2, 4);
                else if (di == DataItem.Wind_Power)
                    return excelRead(p, 3, 4);
                else throw new Exception("Not compatible");
            };

            var hd = readYear(2013);
            hd.AddRange(readYear(2014));
            hd.AddRange(readYear(2015));

            // do here some preprocessing?... interpolate missing and ourliers... but not spikes
            var negvs = hd.Where(x => x.Value < 0).ToList();
            var nulls = hd.Where(x => !x.Value.HasValue).ToList();

            var name = nd.BuildFileName(di, Country.All, Resolution.Hourly);
            nd.SaveFileAsJson(GetAppDataServerPath() + name + ".json", hd);
        }
    }
}