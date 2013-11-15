using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ScriptTools
{
    class ScriptParser
    {
        private string[] fileToLinesDelimiters = new[] { "\r\n" };
        private string[] lineToParamsDelimiters = new[] { "\t", "," };
        private string[] specialDelimeter = new[] { "^:^" };

        public void LoadScripts(string path)
        {
            // for each script file, first get product model from scrip filename; 
            // then parse the content of the script file to get test parameters; 
            // finally verify scripts by datasheet.
            string[] files = Directory.GetFiles(path, "*.ini", SearchOption.AllDirectories);
            string[] testModel = new string[5];
            List<string[]> testParams = null;
            if (files != null)
            {
                foreach (string fileName in files)
                {
                    ParseProductModel(fileName, out testModel);
                    ReadTestScript(fileName, out testParams);
                    //VerifyDatasheet(specsTable, out specsTableStatus);
                }
            }
            else
            {
                MessageBox.Show("Empty directory. Please try again.");
                return;
            }
        }

        private void ParseProductModel(string scriptFileName, out string[] testModel)
        {
            try
            {
                string pattern = @"(?<model>[A-Za-z]+\d{4}[CM]?)(?<mems_cmos_version>\w{4})(?<script_version>[FQ]V\d{3})(?<test_type>\w{2}\d*)(?<is_eng>[E]?\d?)";
                Regex _regex = new Regex(pattern);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(scriptFileName);
                MatchCollection mc = _regex.Matches(fileNameWithoutExtension);
                string[] model = new string[5];

                foreach (Match match in mc)
                {
                    GroupCollection gc = match.Groups;
                    model[0] = gc["model"].ToString();
                    model[1] = gc["mems_cmos_version"].ToString();
                    model[2] = gc["script_version"].ToString();
                    model[3] = gc["test_type"].ToString();
                    model[4] = gc["is_eng"].ToString();
                }
                testModel = model;
            }
            catch (Exception exception)
            {
                testModel = null;
                MessageBox.Show("Parsing product model process failed: \n" + exception.ToString());
                return;
            }
        }

        private void ReadTestScript(string scriptFileName, out List<String[]> testParams)
        {
            //System.IO.FileStream scriptTraceLog = 
            //        new System.IO.FileStream(@"c:\temp\script_log.txt", System.IO.FileMode.OpenOrCreate);
            //System.Diagnostics.TextWriterTraceListener scriptListener = 
            //        new System.Diagnostics.TextWriterTraceListener(scriptTraceLog);

            testParams = new List<string[]>();

            string scriptFileRead;
            List<string> scriptFileLines = new List<string>();
            List<string> scriptFileLinesWithLineNums = new List<string>();
            List<string> scriptErrors = new List<string>();
            string[] scriptFileLineParams;

            try
            {
                // read file content including all delimiters
                StreamReader sr = new StreamReader(scriptFileName);
                scriptFileRead = sr.ReadToEnd();
                sr.Close();

                if (!scriptFileRead.Equals(string.Empty))
                {
                    scriptFileLines.AddRange(scriptFileRead.Split(fileToLinesDelimiters, StringSplitOptions.None));
                    int lineNum = 1;
                    foreach (string line in scriptFileLines)
                    {
                        scriptFileLinesWithLineNums.Add((lineNum++).ToString() + specialDelimeter[0] + line);
                    }

                    // first run the format and bin check, and also remove all comment lines
                    FormatAndBinsChecker(ref scriptFileLinesWithLineNums, out scriptErrors);

                    for (int i = 0; i < scriptFileLinesWithLineNums.Count; i++)
                    {
                        // Current "rules" for commenting in the script file:
                        //   - line starting with "'" usually comments itself
                        //   - multiple comments usually start with "*"
                        if (scriptFileLinesWithLineNums[i].Substring(0, 1) != "'" && (!(scriptFileLinesWithLineNums[i].Contains("*") && scriptFileLines.Contains("="))))
                        {
                            scriptFileLineParams = scriptFileLines[i].Split(lineToParamsDelimiters, StringSplitOptions.None);
                            if (scriptFileLineParams[3].Trim().ToUpper() == "BIN")
                                // skip HW&SW bins collecting
                                continue;
                            else if (scriptFileLineParams[0].Trim().Equals("1"))
                            {
                                string[] testParamPerLine = new string[8];
                                for (int lineParamIndex = 0; lineParamIndex < 12; lineParamIndex++)
                                {
                                    // testType, testName, testUnit, testTarget, testSpecMin, testSpecMax, testSWBin, testHWBin
                                    if (lineParamIndex == 2 && scriptFileLineParams.Length > 2) testParamPerLine[0] = scriptFileLineParams[2].Trim().ToUpper();
                                    if (lineParamIndex == 3 && scriptFileLineParams.Length > 3) testParamPerLine[1] = scriptFileLineParams[3].Trim().ToUpper();
                                    if (lineParamIndex == 4 && scriptFileLineParams.Length > 4) testParamPerLine[2] = scriptFileLineParams[4].Trim().ToUpper();
                                    if (lineParamIndex == 7 && scriptFileLineParams.Length > 7) testParamPerLine[3] = scriptFileLineParams[7].Trim().ToUpper();
                                    if (lineParamIndex == 8 && scriptFileLineParams.Length > 8) testParamPerLine[4] = scriptFileLineParams[8].Trim().ToUpper();
                                    if (lineParamIndex == 9 && scriptFileLineParams.Length > 9) testParamPerLine[5] = scriptFileLineParams[9].Trim().ToUpper();
                                    if (lineParamIndex == 10 && scriptFileLineParams.Length > 10) testParamPerLine[6] = scriptFileLineParams[10].Trim().ToUpper();
                                    if (lineParamIndex == 11 && scriptFileLineParams.Length > 11) testParamPerLine[7] = scriptFileLineParams[11].Trim().ToUpper();
                                }
                                testParams.Add(testParamPerLine);
//                                numberOfTestPoints++;
                            }
                        } // if line starts with "'", skip it
                    } // for loop of each line scanning
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("Reading test script process failed: \n" + exception.ToString());
                return;
            }
        }

        private void FormatAndBinsChecker(ref List<string> scriptLinesWithNums, out List<string> errors)
        {
            List<string> newScriptLines = new List<string>();
            string[] lineDelimiters = new[] { "\t", "," };
            errors = null;
            int counts = scriptLinesWithNums.Count;
            for (int i = 0; i < counts; i++)
            {
                string[] thisLine = scriptLinesWithNums[i].Split(specialDelimeter, StringSplitOptions.None);
                int lineNum = Convert.ToInt16(thisLine[0].Trim());
                // rule 1: the first char of each line can not be space/tab
                string leadingChar = thisLine[1].Substring(0, 1);
                if (lineNum == 1 && !(thisLine[1].Substring(0, 7).Equals("display")))
                {
                    errors.Add(@"script must begin with ""display"" word");
                    continue;
                }
                if (leadingChar.Equals(" ") || leadingChar.Equals("\t")) //this is a comment line
                {
                    errors.Add(@"the first char of each line can not be space/tab\n");
                    continue;
                }
                newScriptLines.Add(thisLine[1]);
            }
            scriptLinesWithNums = newScriptLines;
        }
    }
}
