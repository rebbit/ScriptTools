using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptTools
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadDatasheet();
        }

        private void LoadDatasheet()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "All Excel Files(*.xlsx)|*.xlsx";
            
            DatasheetParser datasheetParser = new DatasheetParser();

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string datasheetFileName = openFileDialog1.FileName.ToString();
                datasheetParser.LoadDataSheetFile(datasheetFileName);
                //show necessary datasheet

                //enable load scripts button and menu item for next step.
                loadScriptsToolStripMenuItem.Enabled = true;
            }

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }
    }
}
