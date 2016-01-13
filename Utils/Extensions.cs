using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class Extensions
    {
        public static double[,] ToRectangularArray(this List<MarketPoint> mps)
        {
            var x = new double[2, mps.Count];
            for (int i = 0; i < mps.Count; i++)
            {
                x[0, i] = (double)mps[i].Volume;
                x[1, i] = (double)mps[i].Price;
            }
            return x;
        }
    }
}
