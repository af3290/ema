using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static MarketModels.Types;

namespace Utils.NordPoolSpot
{
    public class DataDownloader
    {
        public const string MarketCurvesBaseUrl = @"http://www.nordpoolspot.com/globalassets/download-center-market-data/";
        public const string BaseUrl = @"http://www.nordpoolspot.com/globalassets/marketdata-excel-files/";
        public const string SavePath = @"D:\Projects\GitHub\ema\EMA\App_Data\";

        //for equlibrium prices: http://nordpoolspot.com/Market-data1/Elspot/Area-Prices/ALL1/Hourly/?view=table
        //var url = @"http://www.nordpoolspot.com/globalassets/download-center-market-data/mcp_data_report_{0}-00_00_00.xls";
        //http://nordpoolspot.com/globalassets/marketdata-excel-files/production-per-country_2015_daily.xls
        //http://nordpoolspot.com/globalassets/marketdata-excel-files/production-per-country_2015_hourly2.xls

        //Correction: NameAndExtension...
        public string BuildFileName(DataItem dataItem, Resolution resolution, DateTime dateTime)
        {
            var url = "{0}_{1}_{2}.xls";
            url = string.Format(url, dataItem.ToXString(), dateTime.Year, resolution.ToXString());
            return url;
        }
        public string BuildFileName(DataItem dataItem, Country country, Resolution resolution, DateTime dateTime)
        {
            var url = "{0}-{1}_{2}_{3}.xls";
            url = string.Format(url, dataItem.ToXString(), country.ToXString(), dateTime.Year, resolution.ToXString());
            return url;
        }
                
        public string BuildFileName(DataItem dataItem, Resolution resolution, DateTime dateTime, Currency currencye)
        {
            var url = "{0}_{1}_{2}_{3}.xls";
            url = string.Format(url, dataItem.ToXString(), dateTime.Year, resolution.ToXString(), currencye.ToString());
            return url;
        }

        public string BuildFileName(DataItem dataItem, Resolution resolution, Currency currencye)
        {
            var url = "{0}_{1}_{2}";
            url = string.Format(url, dataItem.ToXString(), resolution.ToXString(), currencye.ToString());
            return url;
        }

        public string BuildFileName(DataItem dataItem, Country country, Resolution resolution)
        {
            var url = "{0}_{1}_{2}";
            url = string.Format(url, dataItem.ToXString(), resolution.ToXString(), resolution.ToString());
            return url;
        }

        public string DownloadFile(DataItem dataItem, Country country, Resolution resolution, DateTime dateTime)
        {
            var url = BuildFileName(dataItem, country, resolution, dateTime);
            var file = DownloadFile(url);
            File.WriteAllBytes(SavePath + url, file);
            return url;
        }
        public string DownloadFile(DataItem dataItem, Resolution resolution, DateTime dateTime)
        {
            var url = BuildFileName(dataItem, resolution, dateTime);
            var file = DownloadFile(url);
            File.WriteAllBytes(SavePath + url, file);
            return url;
        }
        public string DownloadFile(DataItem dataItem, Resolution resolution, DateTime dateTime, Currency currency)
        {
            var url = BuildFileName(dataItem, resolution, dateTime, currency);
            var file = DownloadFile(url);
            File.WriteAllBytes(SavePath + url, file);
            return url;
        }

        public void SaveFileAsJson(string filePath, object data)
        {         
            var jsonsx = JsonConvert.SerializeObject(data);
            File.WriteAllText(filePath, jsonsx);
        }
        public byte[] DownloadFile(string url)
        {
            if (url.Contains("mcp_"))
                url = MarketCurvesBaseUrl + url;
            else
                url = BaseUrl + url;

            byte[] data;

            using (WebClient cc = new WebClient())
            {
                data = cc.DownloadData(url);
            }

            return data;
        }

        public void DownloadAndSaveFile(string filePath, string file)
        {
            byte[] data;

            var absoluteUrl = BaseUrl;

            //add the other cases...
            if (file.Contains("mcp_"))
                absoluteUrl = MarketCurvesBaseUrl + file;

            using (WebClient cc = new WebClient())
            {
                data = cc.DownloadData(absoluteUrl);
            }

            File.WriteAllBytes(filePath, data);
        }
    }
}
