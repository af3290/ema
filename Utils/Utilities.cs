using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using HtmlAgilityPack;

namespace Utils
{
    public static class Utilities
    {
        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }
        public static int MonthNumberFrom(string monthName)
        {
            var MonthNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames
                .Select(x=>x.ToLower())
                .ToArray();
            var monthIndex = Array.IndexOf(MonthNames, monthName) + 1;
            return monthIndex;
        }

        public static DateTime TryCastToDateTime(this string str)
        {
            DateTime dt;
            //TODO: there s a big problem here!!!
            var formats = new string[] { "dd/M/yyyy", "dd-M-yyyy", "dd-MM-yyyy", "dd/MM/yyyy",
                "yyyy/MM/dd", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(str, formats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dt))
                return dt;

            try
            {
                dt = DateTime.FromOADate(Double.Parse(str));
                return dt;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static TimeSpan TryCastToTime(this string str)
        {
            DateTime dt;
            //TODO: there s a big problem here!!!
            var formats = new string[] { "H:mm tt", "HH:mm tt", "HH:mm" };
            if (DateTime.TryParseExact(str, formats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dt))
                return dt.TimeOfDay;

            try
            {
                dt = DateTime.Parse(str);
                return dt.TimeOfDay;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static decimal TryCastToDecimal(this string dec)
        {
            if(!string.IsNullOrEmpty(dec))
                dec = dec.Replace(",", ".");

            decimal res = -1;
            Decimal.TryParse(dec, out res);
            return res;
        }

        public static double TryCastToDouble(this string dec)
        {
            if (!string.IsNullOrEmpty(dec))
                dec = dec.Replace(",", ".");

            double res = double.NaN;
            Double.TryParse(dec, out res);
            return res;
        }
        
        public static string StatkraftTableTotalProduction(this HtmlNode n)
        {
            return n.ChildNodes
                .Where(c => c.Name.Equals("td"))
                .ToList()[7]
                .InnerText.Replace("&nbsp", "");
        }
    }
}