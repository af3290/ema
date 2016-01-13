using System;

namespace Utils
{
    public class MarketPoint
    {
        public decimal Price;
        public decimal Volume;

        public Point<decimal> ToPoint()
        {
            return ToPoint(false);
        }

        public override bool Equals(object obj)
        {
            var p = obj as MarketPoint;

            if (p != null)
            {
                return ToPoint().Equals(p.ToPoint());
            }

            return base.Equals(obj);
        }

        public Point<decimal> ToPoint(bool reverse)
        {
            return new Point<decimal>(reverse ? Volume : Price , reverse ? Price : Volume);
        }
    }
}