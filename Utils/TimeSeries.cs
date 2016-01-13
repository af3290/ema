using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class TimeSeries
    {
        private DateTime? _maturity;

        public TimeSeries()
        {
            Prices = new List<HistoricalPrice>();
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? Maturity {
            get {
                if (Prices.Count == 0)
                    return null;
               
                if (_maturity == null) {
                    var mat = Prices.Last().DateTime;
                    _maturity = mat.AddDays(-mat.Day+1).AddMonths(1); //+1 for 1st of month
                }

                return _maturity;
            } set {

            }
        }
        public List<HistoricalPrice> Prices { get; set; }
    }
}
