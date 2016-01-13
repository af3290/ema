using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.NordPoolSpot
{
    public class ExcelReader
    {
        public static void ReadMWhFile(string fileName)
        {
            var _excelApp = new Microsoft.Office.Interop.Excel.Application();

            var workBook = _excelApp.Workbooks.Open(fileName,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing);

            var n = workBook.Sheets.Count;

            Microsoft.Office.Interop.Excel.Worksheet sheet = workBook.ActiveSheet;

            //get maximum
            var range = sheet.get_Range("A4:B8764", Type.Missing);
            var data = (object[,])range.Value2;
        }

        public void ReadPricesFile()
        {

        }
    }
}
