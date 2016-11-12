using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.Model;
using static MarketModels.Types;
using HtmlAgilityPack;
using System.Net;
using System.Collections.Specialized;

namespace Utils.NasdaqOMX
{
    public class Downloader
    {
        public const string FeedURL = "http://www.nasdaqomx.com/webproxy/DataFeedProxyIRC1.aspx";
        public const string XmlQuery = "<post><param name=\"Exchange\" value=\"NMF\"/><param name=\"SubSystem\" value=\"Prices\"/><param name=\"Action\" value=\"GetMarket\"/><param name=\"Market\" value=\"GITS:NC:ENO\"/><param name=\"inst__an\" value=\"id,tp,nm,fnm,bp,ap,lsp,spch,spchp,hp,lp,tv,rq,onexv,stlpr,oi,upc,t,exfdt,dlt\"/><param name=\"ext_xslt\" value=\"nordpool-v2/inst_table.xsl\"/><param name=\"empdata\" value=\"false\"/><param name=\"XPath\" value=\"//inst[@rq!='' or @tv!='']\"/><param name=\"ext_xslt_options\" value=\"\"/><param name=\"ext_xslt_notlabel\" value=\"fnm\"/><param name=\"ext_xslt_hiddenattrs\" value=\",not,lnot,isp,isc,exfdt,dlt,tp,\"/><param name=\"ext_xslt_lang\" value=\"en\"/><param name=\"ext_xslt_tableId\" value=\"derivatesNordicTable\"/><param name=\"ext_xslt_tableClass\" value=\"tablesorter\"/><param name=\"ext_xslt_market\" value=\"GITS:NC:ENO\"/><param name=\"app\" value=\"www.nasdaqomx.com//commodities/market-prices\"/></post>";

        public List<ForwardContract> ForwardCurve(bool nonOverlapping)
        {
            string result;

            using (WebClient cc = new WebClient())
            {
                byte[] response = cc.UploadValues(FeedURL,
                    new NameValueCollection() {
                       { "xmlquery", XmlQuery }
                });

                result = System.Text.Encoding.UTF8.GetString(response);
            }

            HtmlDocument htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(result);
            var nordicTable = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='derivatesNordicTable']/tbody");

            var enos = nordicTable.ChildNodes.Where(x =>
                   x.Id.Contains("ENOD") || x.Id.Contains("ENOW")
                || x.Id.Contains("ENOM") || x.Id.Contains("ENOQ")
                || x.Id.Contains("ENOYR")
            ).ToList();

            var forwardContracts = enos.Select(x => new ForwardContract()
            {
                Contract = x.Id.Replace("derivatesNordicTable-NOP", ""),
                Bid = GetFromTD(x, "bp").TryCastToDecimal(),
                Ask = GetFromTD(x, "ap").TryCastToDecimal(),
                Volume = GetFromTD(x, "onexv").TryCastToDecimal(),
                LastPrice = GetFromTD(x, "lsp").TryCastToDecimal(),
                FixPrice = GetFromTD(x, "stlpr").TryCastToDecimal()
            }).ToList();

            foreach (var contract in forwardContracts)
            {
                var lastIdx = contract.Contract.Contains("ENOYR") ? 5 : 4;
                var prefix = contract.Contract.Substring(0, lastIdx);
                var id = contract.Contract.Substring(lastIdx);
                int year = 2000 + int.Parse(id.Substring(id.Length - 2)),
                    month,
                    day;

                switch (prefix)
                {
                    case "ENOD":
                        day = int.Parse(id.Substring(0, 2));
                        month = int.Parse(id.Substring(2, 2));
                        contract.Begin = new DateTime(year, month, day, 00, 00, 00);
                        contract.End = new DateTime(year, month, day, 23, 59, 59);
                        contract.Resolution = Resolution.Daily;
                        break;
                    case "ENOW":
                        int weekNb = int.Parse(id.Substring(0, 2));
                        var firstDay = Utilities.FirstDateOfWeekISO8601(year, weekNb);
                        contract.Begin = firstDay;
                        contract.End = firstDay.AddDays(7).Subtract(new TimeSpan(0, 0, 1));
                        contract.Resolution = Resolution.Weekly;
                        break;
                    case "ENOM":
                        var monthName = id.Substring(0, 3).ToLower();
                        month = Utilities.MonthNumberFrom(monthName);
                        contract.Begin = new DateTime(year, month, 1, 00, 00, 00);
                        contract.End = contract.Begin.AddDays(DateTime.DaysInMonth(year, month)).Subtract(new TimeSpan(0, 0, 1));
                        contract.Resolution = Resolution.Monthly;
                        break;
                    case "ENOQ":
                        int quarterNb = int.Parse(id.Substring(0, 1));
                        contract.Begin = new DateTime(year, (quarterNb - 1) * 3 + 1, 1, 00, 00, 00);
                        contract.End = contract.Begin.AddMonths(3).Subtract(new TimeSpan(0, 0, 1));
                        contract.Resolution = Resolution.Quarterly;
                        break;
                    case "ENOYR":
                        contract.Begin = new DateTime(year, 1, 1, 00, 00, 00);
                        contract.End = contract.Begin.AddYears(1).Subtract(new TimeSpan(0, 0, 1));
                        contract.Resolution = Resolution.Yearly;
                        break;
                    default:
                        break;
                }
            }

            /* Include only high resolution contracts by eliminating low resolution overlapping ones */
            if (nonOverlapping)
            {
                var years = forwardContracts.Where(c => c.Resolution == Resolution.Yearly).ToList();
                var quarters = forwardContracts.Where(c => c.Resolution == Resolution.Quarterly).ToList();
                var months = forwardContracts.Where(c => c.Resolution == Resolution.Monthly).ToList();

                years.ForEach(year =>
                {
                    if (quarters.Count(quarter => quarter.Begin.Year == year.Begin.Year) == 4)
                        forwardContracts.Remove(year);
                });

                quarters.ForEach(quarter =>
                {
                    if (months.Count(month => quarter.Begin <= month.Begin && month.End <= quarter.End) == 3)
                        forwardContracts.Remove(quarter);
                });
            }

            var discontinuityIndices = new List<int>();
            var subFwdContracts = forwardContracts.Skip(1).ToList();
            for (int i = 0; i < subFwdContracts.Count(); i++)
            {
                var forwardContract = subFwdContracts[i];
                if (forwardContract.Begin - forwardContracts[i].End > TimeSpan.FromDays(1))
                    discontinuityIndices.Add(i);
            }

            //handle discountinuous forward contracts! IT CAN HAPPEN!
            //handling strategy (others are possible) - extent the latest to cover
            //interpolate...

            return forwardContracts; 
        }

        private string GetFromTD(HtmlNode tr, string tdByName)
        {
            var td = tr.ChildNodes.SingleOrDefault(y => y.Name.Equals("td") && y.Attributes["name"].Value.Equals(tdByName));
            return td == null ? null : td.InnerText;
        }
    }
}
