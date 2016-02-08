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

        /// <summary>
        /// Interprets the lists as columns lists and makes a corresponding matrix
        /// </summary>
        /// <param name="lists"></param>
        /// <returns></returns>
        public static double[,] ColumnsToRectangularArray(this List<List<double>> lists)
        {
            if (lists.Count == 0)
                return new double[0, 0];
            
            var exData = new double[lists[0].Count, lists.Count];

            for (int i = 0; i < lists.Count; i++)
            {
                for (int j = 0; j < lists[i].Count; j++)
                {
                    exData[j, i] = lists[i][j];
                }
            }

            return exData;
        }

        public static bool HasInvalidData(this double[] vec)
        {
            return vec.Any(x => double.IsNaN(x) || double.IsInfinity(x));
        }

        public static bool HasInvalidData(this double[,] mat)
        {
            //for (int i = 0; i < mat.Count; i++)
            //{
            //    for (int j = 0; j < lists[i].Count; j++)
            //    {
            //        exData[j, i] = lists[i][j];
            //    }
            //}
            var flattened = mat.Cast<double>().ToArray();
            return flattened.HasInvalidData();
        }
    }
}
