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
        public void LoadDataSheetFile(string datasheetFileName, out List<Product> productLists)
        {
            ExcelInit(datasheetFileName, out productLists);
            ExcelClose();
        }


        //Method to initialize opening Excel
        private void ExcelInit(String path, out List<Product> idinfo)
        {
            xlApp = new Excel.Application();
            string dsSheet = "datasheet";
            string idinfoSheet = "idinfo";
            idinfo = null;
            if (System.IO.File.Exists(path))
            {
                // then go and load this into excel
                xlWorkBook = xlApp.Workbooks.Open(path,
                0, true, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t",
                false, false, 0, true, 1, 0);

                //read info datasheet first to generate product specs list
                info_xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(idinfoSheet);
                idinfo = ReadIdInfoIntoDataTable(info_xlWorkSheet);
                //read datasheet and load the data into product specs list
                //ds_xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(dsSheet);
                //ReadDatasheetIntoDataTable(ds_xlWorkSheet);
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
        private List<Product> ReadIdInfoIntoDataTable(Excel._Worksheet sheet)
        {
            List<Product> listProducts = new List<Product>();

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
            string [] sep2 = {","};
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
            if (idinfo != null)
            {
                foreach (string prod in idinfo)
                {
                    val = prod.Split(',');
                    FamilyName familyname = GetFamilyName(val[0].Trim());
                    ProductName productname = GetProductName(val[1].Replace("-","").Trim());
                    byte whoami = Convert.ToByte(val[2].Trim(), 16);
                    int continuity = Convert.ToInt16(val[3].Trim());
                    Product product = new Product(familyname, productname, whoami, continuity);
                    listProducts.Add(product);
                }
            }
            return listProducts;
        }
        private byte ConvertStringByte(string stringVal)
        {
            byte byteVal = 0;

            try
            {
                byteVal = Convert.ToByte(stringVal);
            }
            catch (System.OverflowException)
            {
                MessageBox.Show("Conversion from string to byte overflowed.");
            }
            catch (System.FormatException)
            {
                MessageBox.Show("The string is not formatted as a byte.");
            }
            catch (System.ArgumentNullException)
            {
                MessageBox.Show("The string is null.");
            }
            return byteVal;
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


        private ProductName GetProductName(string productname)
        {
            ProductName pn;
            switch (productname)
            {
                case "MPU6500":
                    pn = ProductName.MPU6500;
                    break;
                case "MPU6500M":
                    pn = ProductName.MPU6500M;
                    break;
                case "MPU6505":
                    pn = ProductName.MPU6505;
                    break;
                case "MPU6505C":
                    pn = ProductName.MPU6505C;
                    break;
                case "MPU6505M":
                    pn = ProductName.MPU6505M;
                    break;
                case "MPU6506":
                    pn = ProductName.MPU6506;
                    break;
                case "MPU6515":
                    pn = ProductName.MPU6515;
                    break;
                case "MPU6515C":
                    pn = ProductName.MPU6515C;
                    break;
                case "MPU6515M":
                    pn = ProductName.MPU6515M;
                    break;
                case "MPU6520":
                    pn = ProductName.MPU6520;
                    break;
                case "MPU6521":
                    pn = ProductName.MPU6521;
                    break;
                case "MPU6530":
                    pn = ProductName.MPU6530;
                    break;
                case "MPU6580":
                    pn = ProductName.MPU6580;
                    break;
                case "MPU6700":
                    pn = ProductName.MPU6700;
                    break;
                case "MPU7400":
                    pn = ProductName.MPU7400;
                    break;
                case "MPU9250":
                    pn = ProductName.MPU9250;
                    break;
                case "MPU9350":
                    pn = ProductName.MPU9350;
                    break;
                case "ISZ2530":
                    pn = ProductName.ISZ2530;
                    break;
                case "ISX2530":
                    pn = ProductName.ISX2530;
                    break;
                case "IDG2530":
                    pn = ProductName.IDG2530;
                    break;
                case "IDG2030":
                    pn = ProductName.IDG2030;
                    break;
                case "IXZ2530":
                    pn = ProductName.IXZ2530;
                    break;
                case "IXZ2030":
                    pn = ProductName.IXZ2030;
                    break;
                default:
                    pn = ProductName.NA;
                    break;
            }
            return pn;
        }
        private FamilyName GetFamilyName(string familyname)
        {
            FamilyName fn;
            switch (familyname)
            {
                case "Scorpion":
                    fn = FamilyName.Scorpion;
                    break;
                case "Fluorite":
                    fn = FamilyName.Fluorite;
                    break;
                case "Opal":
                    fn = FamilyName.Opal;
                    break;
                case "Sapphire":
                    fn = FamilyName.Sapphire;
                    break;
                case "Turquoise":
                    fn = FamilyName.Turquoise;
                    break;
                case "Amber":
                    fn = FamilyName.Amber;
                    break;
                default:
                    fn = FamilyName.NA;
                    break;
            }
            return fn;
        }


    }
}
