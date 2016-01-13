using Excel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using DataTable = System.Data.DataTable;

namespace Utils
{
    public class ExcelReader
    {

        /// <summary>
        /// X = Price , Y = Volume, Z - Hour 
        /// Transform the X and Y values so that they will match a grid structure,
        /// starting from an uneven structure.
        /// Assumes ordered points
        /// </summary>
        /// <param name="pcms"></param>
        /// <returns></returns>
        public List<PriceCurvesModel> NordPoolSpotVolumeCurvesToGrid(List<PriceCurvesModel> pcms)
        {
            /* Compute box values */
            var minVolumeDemand = pcms.Min(x => x.DemandCurve.Min(y => y.Volume));
            var minVolumeSupply = pcms.Min(x => x.SupplyCurve.Min(y => y.Volume));
            var minVolume = minVolumeDemand <= minVolumeSupply ? minVolumeDemand : minVolumeSupply;

            var maxVolumeDemand = pcms.Max(x => x.DemandCurve.Max(y => y.Volume));
            var maxVolumeSupply = pcms.Max(x => x.SupplyCurve.Max(y => y.Volume));
            var maxVolume = maxVolumeDemand >= maxVolumeSupply ? maxVolumeDemand : maxVolumeSupply;

            /* Compute granularity */
            var maxCountDemand = pcms.Max(x => x.DemandCurve.Count);
            var maxCountSupply = pcms.Max(x => x.SupplyCurve.Count);
            var maxCount = maxCountDemand >= maxCountSupply ? maxCountDemand : maxCountSupply;

            /* Compute values using linear interpolation interpolations */
            var volumeStep = (maxVolume - minVolume) / maxCount;
            
            var result = new List<PriceCurvesModel>();

            foreach (var priceCurvesModel in pcms)
            {
                var newPriceCurveModel = new PriceCurvesModel() { Hour = priceCurvesModel.Hour };

                /* Must have more than 2 sample points */
                if (priceCurvesModel.DemandCurve.Count < 3 || priceCurvesModel.SupplyCurve.Count < 3)
                    continue;

                for (int i = 0; i <= maxCount; i++)
                {
                    var volume = minVolume + i * volumeStep;
                    var price = Interpolate(priceCurvesModel.DemandCurve, volume);
                    newPriceCurveModel.SupplyCurve.Add(new MarketPoint() { Volume = volume, Price = price });
                    
                    price = Interpolate(priceCurvesModel.SupplyCurve, volume);
                    newPriceCurveModel.DemandCurve.Add(new MarketPoint() {Volume = volume, Price = price});
                }

                result.Add(newPriceCurveModel);
            }

            return result;
        }

        public decimal Interpolate(List<MarketPoint> supplyDemandPoints, decimal volume)
        {
            var floor = supplyDemandPoints.LastOrDefault(x => x.Volume <= volume);
            var ceil = supplyDemandPoints.FirstOrDefault(x => x.Volume >= volume);

            decimal price = -1.0m;

            /* Lower outer value */
            if (floor == null)
            {
                var first = supplyDemandPoints.First();
                var postFirst = supplyDemandPoints.Skip(1).First();
                
            }

            /* Upper outer value */
            if (ceil == null)
            {
                var last = supplyDemandPoints.Last();
                var preLast = supplyDemandPoints[supplyDemandPoints.Count - 2];

                //TODO: use linear interpolation library here... it makes it cleaner...
            }

            /* Inner value */

            return price;
        }

        public List<PriceCurvesModel> ReadNordPoolSpotPriceCurves(Stream excelFile)
        {
            var excelReader = ExcelReaderFactory.CreateBinaryReader(excelFile);

            var result = excelReader.AsDataSet();

            var curves = result.Tables[0];

            var demandCurvesStartingPoints = CoordinatesOfTermInTable(curves, "Buy curve");
            var supplyCurvesStartingPoints = CoordinatesOfTermInTable(curves, "Sell curve");

            if (demandCurvesStartingPoints.Count != supplyCurvesStartingPoints.Count)
                throw new Exception("Excel file has inconsistent data");

            var pcmList = new List<PriceCurvesModel>();

            for (int i = 0; i < demandCurvesStartingPoints.Count; i++)
            {
                var pcm = new PriceCurvesModel() { Hour = i };

                var demandPoint = demandCurvesStartingPoints[i];
                var demandRow = demandPoint.X + 1;

                /* Columns are identical, because both are on same column in the spreadsheet */
                var col = demandPoint.Y + 1;
                var supplyRow = supplyCurvesStartingPoints[i].X + 1;

                /* Demands data */
                for (int j = demandRow; j < supplyCurvesStartingPoints[i].X; j += 2)
                {
                    var price = curves.Rows[j].ItemArray[col].ToString().TryCastToDecimal();
                    var volume = curves.Rows[j + 1].ItemArray[col].ToString().TryCastToDecimal();

                    pcm.DemandCurve.Add(new MarketPoint() { Price = price, Volume = volume });
                }

                /* Ensure ordering */
                pcm.DemandCurve = pcm.DemandCurve.OrderBy(d => d.Volume).ToList();

                /* Supply data */
                for (int j = supplyCurvesStartingPoints[i].X + 1; j < curves.Rows.Count; j += 2)
                {
                    var po = curves.Rows[j].ItemArray[col];
                    var vo = curves.Rows[j + 1].ItemArray[col];
                    
                    /* Reached bottom of spreadsheet*/
                    if (string.IsNullOrEmpty(po.ToString()) || string.IsNullOrEmpty(vo.ToString()))
                        break;

                    var price = po.ToString().TryCastToDecimal();
                    var volume = vo.ToString().TryCastToDecimal();

                    pcm.SupplyCurve.Add(new MarketPoint() { Price = price, Volume = volume });
                }

                /* Ensure ordering */
                pcm.SupplyCurve = pcm.SupplyCurve.OrderBy(d => d.Volume).ToList();

                /* Must happen! */
                pcm.CalculateEquilibrium();

                pcmList.Add(pcm);
            }

            return pcmList; //0.5 seconds for this shit... TOO MUCH!
        }

        //could be more optimized... but do we need it to be more optimized?
        private List<Point<int>> CoordinatesOfTermInTable(DataTable dt, string term)
        {
            var res = new List<Point<int>>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var r = dt.Rows[i];
                for (int j = 0; j < r.ItemArray.Length; j++)
                {
                    var cellValue = r.ItemArray[j];

                    if (cellValue != null && cellValue.ToString().Equals(term))
                    {
                        res.Add(new Point<int>(i, j));
                    }
                }
            }

            /* Reorder ascending by columns, that's what we need! */
            res = res.OrderBy(p => p.Y).ToList();

            return res;
        }

        public List<HistoricalPrice> ReadNordPoolSpotHistoricalPrices(Stream stream)
        {
            var excelReader = ExcelReaderFactory.CreateBinaryReader(stream);

            var result = excelReader.AsDataSet();

            var historicalPrices = result.Tables[0];

            var hours = CoordinatesOfTermInTable(historicalPrices, "Hous");
            var containsHours = hours.Count > 0;

            var sysIndex = CoordinatesOfTermInTable(historicalPrices, "SYS")[0].X + 1;
            
            var hPrices = new List<HistoricalPrice>();

            var priceValue = "1";

            while (!string.IsNullOrEmpty(priceValue)) {
                priceValue = historicalPrices.Rows[sysIndex].ItemArray[containsHours ? 2 : 1].ToString();

                var date = historicalPrices.Rows[sysIndex].ItemArray[0].ToString();
                var dt = DateTime.Parse(date);

                var pv = new HistoricalPrice() {
                    Value = priceValue.TryCastToDecimal(),
                    DateTime = dt
                };

                sysIndex++;
            }

            return hPrices;
        }

        public List<HistoricalPrice> ReadNordPoolSpotHistoricalPricesInterop(string fileName, int colNr = 2)
        {
            var _excelApp = new Microsoft.Office.Interop.Excel.Application();

            var workBook = _excelApp.Workbooks.Open(fileName,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing);

            var n = workBook.Sheets.Count;

            Microsoft.Office.Interop.Excel.Worksheet sheet = workBook.ActiveSheet;

            //get maximum, a little over 24*365...
            var range = sheet.get_Range("A4:Q9000", Type.Missing);
            var data = (object[,])range.Value2;
            var rowIdx = 0;

            var list = new List<HistoricalPrice>();
            var start = data[1, 1].ToString().TryCastToDateTime();
            while (rowIdx++>=0)
            {
                if (data[rowIdx, 1] ==null || string.IsNullOrEmpty(data[rowIdx, 1].ToString()))
                    break;

                DateTime dt;

                if (fileName.ToLower().Contains("daily"))
                    dt = start.AddDays(rowIdx - 1); //- 1 becayse data starts at 1
                else if (fileName.ToLower().Contains("hourly"))
                    dt = start.AddHours(rowIdx - 1);
                else
                    //THIS IS VERY CRAPPY, because of datetime fromats mess!
                    dt = data[rowIdx, 1].ToString().TryCastToDateTime();

                //TODO: figure out why it gets odd values from times ahead...
                if (dt >= DateTime.Today)
                    continue;

                decimal? p = null; 

                if (data[rowIdx, colNr] != null && !string.IsNullOrEmpty(data[rowIdx, colNr].ToString()))
                    p = data[rowIdx, colNr].ToString().TryCastToDecimal();

                list.Add(new HistoricalPrice() {DateTime = dt, Value = p});
            }

            return list;
        }
    }
}
