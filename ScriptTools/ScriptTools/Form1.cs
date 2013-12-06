using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Security.Permissions;

namespace ScriptTools {
    public partial class Form1 : Form {
        List<Product> productLists = new List<Product>();
        List<string> scriptErrors = new List<string>();
        public static int fileNum = 0;
        public Form1() {
            InitializeComponent();
            InitializeDataProcess();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            System.Windows.Forms.Application.Exit();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            LoadDatasheet();
        }

        private void loadScriptsToolStripMenuItem_Click(object sender, EventArgs e) {
            LoadScripts();
        }

        private void InitializeDataProcess() {
            List<Product> initProductLists = new List<Product>();
            DatasheetParser datasheetParser = new DatasheetParser();
            //pre-load the default datasheet
            string startupPath = Environment.CurrentDirectory;
            string defaultDatasheetFileName = startupPath.Replace("bin\\Debug", "") + @"..\datasheet.xlsx";
            datasheetParser.LoadDataSheetFile(defaultDatasheetFileName, out initProductLists);
            //append to the global list
            productLists.AddRange(initProductLists);
        }
        private void LoadDatasheet() {
            DatasheetParser datasheetParser = new DatasheetParser();
            List<Product> newProductLists = new List<Product>();
            using (OpenFileDialog openFileDialog1 = new OpenFileDialog()) {
                openFileDialog1.Filter = "All Excel Files(*.xlsx)|*.xlsx";
                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    string datasheetFileName = openFileDialog1.FileName.ToString();
                    datasheetParser.LoadDataSheetFile(datasheetFileName, out newProductLists);
                    //append the new product list to the existing one
                    productLists.AddRange(newProductLists);
                    //todo: show product list

                    //todo: show datasheet

                }
            }
        }

        private void LoadScripts() {
            string startupPath = Application.StartupPath;
            using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                ScriptParser scriptParser = new ScriptParser();
                dialog.Description = "Open a script folder";
                dialog.ShowNewFolderButton = true;
                //dialog.RootFolder = Environment.SpecialFolder.Desktop;
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = Environment.CurrentDirectory;
                
                if (dialog.ShowDialog() == DialogResult.OK) {
                    scriptParser.LoadScripts(dialog.SelectedPath, ref scriptErrors);
                }
                if (scriptErrors != null) {
                    //enable results window
                    tabControl1.Visible = Enabled;
                    treeView1.Visible = Enabled;
                    button1.Visible = Enabled;
                    button2.Visible = Enabled;
                    UpdateTreeViews();
                }
            }

        }

 
        private void UpdateTreeViews() {
            treeView1.BeginUpdate();
            int currNode = fileNum;
            int nextNode = fileNum;
            string filenameLineMarker = ".ini";
            for (int i = 0; i < scriptErrors.Count; i++) {
                string line = scriptErrors[i];
                if (line.Contains(filenameLineMarker)) {
                    TreeNode rootNode = new TreeNode(line);
                    rootNode.ForeColor = Color.Green;
                    treeView1.Nodes.Add(rootNode);
                    nextNode++;
                    currNode++;
                    fileNum++;
                    continue;
                }
                treeView1.Nodes[currNode - 1].ForeColor = Color.Red;
                TreeNode childNode = new TreeNode(line);
                childNode.ForeColor = Color.Red;
                treeView1.Nodes[currNode - 1].Nodes.Add(childNode);
            }
            treeView1.ExpandAll();
            treeView1.EndUpdate();
        }

        private void button1_Click(object sender, EventArgs e) {
            //clear error list, load the script files, and re-run the checks

        }

        private void button2_Click(object sender, EventArgs e) {
            System.Windows.Forms.Application.Exit();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e) {


        }






    }
}
