using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using System.Windows.Forms;

namespace ScriptTools
{
    class DatasheetParser
    {
        private static Excel._Application xlApp;
        private static Excel._Workbook xlWorkBook = null;
        private static Excel._Worksheet ds_xlWorkSheet = null;
        private static Excel._Worksheet info_xlWorkSheet = null;

        public void LoadDataSheetFile(string datasheetFileName)
        {
            ExcelInit(datasheetFileName);

            //datasheet processing example
            string test = ExcelGetValue("A8", ds_xlWorkSheet);

            ExcelClose();
        }


        //Method to initialize opening Excel
        static void ExcelInit(String path)
        {
            xlApp = new Excel.Application();
            string dsSheet = "datasheet";
            string idinfoSheet = "idinfo";

            if (System.IO.File.Exists(path))
            {
                // then go and load this into excel
                xlWorkBook = xlApp.Workbooks.Open(path,
                0, true, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t",
                false, false, 0, true, 1, 0);
                ds_xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(dsSheet);
                info_xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(idinfoSheet);
            }
            else
            {
                MessageBox.Show("Unable to open excel file!");
                System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
                xlApp = null;
                System.Windows.Forms.Application.Exit();
            }

        }

        //Method to get value; cellname is A1,A2, or B1,B2 etc...in excel.
        static string ExcelGetValue(string cellname, Excel._Worksheet sheet)
        {
            string value = string.Empty;
            try
            {
                value = sheet.get_Range(cellname).get_Value().ToString();
            }
            catch
            {
                value = "";
            }

            return value;
        }

        //Method to close excel connection
        static void ExcelClose()
        {
            if (xlApp != null)
            {
                try
                {
                    xlWorkBook.Close();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
                    xlApp = null;
                    xlWorkBook = null;
                }
                catch (Exception ex)
                {
                    xlApp = null;
                    MessageBox.Show("Unable to release the Object " + ex.ToString());
                }
                finally
                {
                    GC.Collect();
                }
            }
        }


    }
}
