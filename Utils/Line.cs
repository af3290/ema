using System;

namespace Utils
{
    /// <summary>
    /// Shorthand class wrapping decimals to doubles...
    /// </summary>
    public class Line
    {
        public double X1, Y1, X2, Y2;

        public Line(decimal x1, decimal y1, decimal x2, decimal y2)
        {
            X1 = (double)x1;
            Y1 = (double)y1;
            X2 = (double)x2;
            Y2 = (double)y2;
        }

        public static double FindLength(decimal X1, decimal Y1 , decimal X2 , decimal Y2)
        {
            return Math.Sqrt(Math.Pow((double) (X2 - X1), 2) + Math.Pow((double) (Y2 - Y1), 2));
        }

        public static decimal FindSlope(decimal X1, decimal Y1, decimal X2, decimal Y2)
        {
            if ((double)X2 - (double)X1 < 1e-5)
                X2 = X1 + (decimal) 1e-5;

            return (decimal) ((double)(Y2 - Y1)/(double)(X2 - X1));
        }

        public Point<decimal> Intersect(Line l)
        {
            double X3 = l.X1, X4 = l.X2;
            double Y3 = l.Y1, Y4 = l.Y2;

            var divisor = (X1 - X2) * (Y3 - Y4) - (Y1 - Y2) * (X3 - X4);

            if (Math.Abs(divisor) < 1e-8)
                throw new InvalidOperationException("Jesus Christ! you're intersecting two parallel lines...");

            //don't use matrices... to complicate just for a nice solution...
            var x = (X1 * Y2 - Y1 * X2) * (X3 - X4) - (X3 * Y4 - Y3 * X4) * (X1 - X2);
            var y = (X1 * Y2 - Y1 * X2) * (Y3 - Y4) - (X3 * Y4 - Y3 * X4) * (Y1 - Y2);

            return new Point<decimal>((decimal)(x / divisor), (decimal) (y/divisor));
        }
    }
}