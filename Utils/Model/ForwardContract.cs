using System;
using MarketModels;
using static MarketModels.Types;

namespace Utils.Model
{
    public class ForwardContract
    {
        public string Contract { get; set; }
        public string ContractS { get { return Contract.Replace("ENO", ""); } }
        public Resolution Resolution { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public int Hours { get { return (int)(End - Begin).TotalHours; } }
        public decimal FixPrice { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Volume { get; set; }
    }
}