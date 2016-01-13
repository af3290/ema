using System;
using MarketModels;
using static MarketModels.Types;

namespace Utils.Model
{
    public class ForwardContract
    {
        public string Contract { get; set; }
        public Resolution Resolution { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public decimal FixPrice { get; set; }
        public decimal LastPrice { get; set; }
    }
}