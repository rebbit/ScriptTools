using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using System.Windows.Forms;
using System.Collections;

namespace ScriptTools
{
    class DatasheetParser
    {
        private Excel._Application xlApp;
        private Excel._Workbook xlWorkBook = null;
        private Excel._Worksheet ds_xlWorkSheet = null;
        private Excel._Worksheet info_xlWorkSheet = null;
        public void LoadDataSheetFile(string datasheetFileName)
        {
            ExcelInit(datasheetFileName);
            ExcelClose();
        }


        //Method to initialize opening Excel
        private void ExcelInit(String path)
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

                //read info datasheet first to generate product specs list
                info_xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(idinfoSheet);
                List<string> idinfo = ReadIdInfoIntoDataTable(info_xlWorkSheet);
                //read datasheet and load the data into product specs list
                ds_xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(dsSheet);
                ReadDatasheetIntoDataTable(ds_xlWorkSheet);


            }
            else
            {
                MessageBox.Show("Unable to open excel file!");
                System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
                xlApp = null;
                System.Windows.Forms.Application.Exit();
            }

        }

        //currently the excel sheets(ds and info) data structure are hard-coded, 
        //will improve later
        private List<string> ReadIdInfoIntoDataTable(Excel._Worksheet sheet)
        {
            //sheet header:
            //Device Family	| Product | SW_WHOAMI | Production SWRev | ES SWRev | Continuity
            string[] header = { "Device Family", "Product", "SW_WHOAMI", "Production SWRev", "ES SWRev", "Continuity" };
            int posDeviceFamily = 1;
            int posProduct = 2;
            int posSWWhoAmI = 3;
            int posContinuity = 6;
            List<string> list = new List<string>();
            string[] val = new string[4];
            string sep = ", ";
            List<string> idinfo = new List<string>();
            if (sheet != null)
            {
                //create a list without any duplicate
                Excel.Range last = sheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing);
                Excel.Range range = sheet.get_Range("A1", last);
                int rows = last.Row;
                string cellName = string.Empty;
                string cellVal = string.Empty;
                for (int i = 2; i < rows; i++)
                {
                    val[0] = ExcelGetValue(GetExcelColumnName(posDeviceFamily) + i.ToString(), sheet);
                    val[1] = ExcelGetValue(GetExcelColumnName(posProduct) + i.ToString(), sheet);
                    val[2] = ExcelGetValue(GetExcelColumnName(posSWWhoAmI) + i.ToString(), sheet);
                    val[3] = ExcelGetValue(GetExcelColumnName(posContinuity) + i.ToString(), sheet);
                    list.Add(String.Join(sep, val));
                }
                //now remove duplicates
                idinfo = list.Distinct().ToList();
            }
            return idinfo;
        }
        private void ReadDatasheetIntoDataTable(Excel._Worksheet sheet)
        {
            int cols, rows;
            if (sheet != null)
            {
                cols = sheet.Columns.Count;
                rows = sheet.Rows.Count;
                string cellName = string.Empty;
                string cellVal = string.Empty;
                for (int i = 1; i < rows; i++)
                {
                    for (int j = 1; j <= cols; j++)
                    {
                        cellName = GetExcelColumnName(j) + i.ToString();
                        cellVal = ExcelGetValue(cellName, sheet);
                    }
                }
            }
        }

        private string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }
        //Method to get value; cellname is A1,A2, or B1,B2 etc...in excel.
        private string ExcelGetValue(string cellname, Excel._Worksheet sheet)
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
        private void ExcelClose()
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
