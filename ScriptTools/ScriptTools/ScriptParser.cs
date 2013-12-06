using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ScriptTools {
    class ScriptParser {
        private string[] fileToLinesDelimiters = new[] { "\r\n" };
        private string[] lineToParamsDelimiters = new[] { "\t", "," };
        private string[] specialDelimeter = new[] { "^:^" };
        private int[] reservedBins = new[] { -999, -998, -997, -996, -995, -994, -993, -992, -991, -990 };

        public void LoadScripts(string path, ref List<string> scriptErrors) {
            // for each script file, first get product model from scrip filename; 
            // then parse the content of the script file to get test parameters; 
            // finally verify scripts by datasheet.
            scriptErrors = new List<string>();
            string[] files = Directory.GetFiles(path, "*.ini", SearchOption.AllDirectories);
            string[] testModel = new string[5];
            List<ScriptDefs> testParams = null;
            List<string> testScriptErrors;

            if (files != null) {
                foreach (string fileName in files) {
                    ParseProductModel(fileName, out testModel);
                    scriptErrors.Add(fileName);
                    sType scriptType = ParseScriptType(testModel);
                    ReadTestScript(fileName, scriptType, out testParams, out testScriptErrors);
                    if (testScriptErrors != null) {
                        scriptErrors.AddRange(testScriptErrors);
                    }
                    //VerifyDatasheet(specsTable, out specsTableStatus);
                }
            } else {
                MessageBox.Show("Empty directory. Please try again.");
                return;
            }
        }

        private void ParseProductModel(string scriptFileName, out string[] testModel) {
            try {
                string pattern = @"(?<model>[A-Za-z]+\d{4}[CM]?)(?<mems_cmos_version>\w{4})(?<script_version>[FQ]V\d{3})(?<script_type>(\w{2}\d)?)(?<is_eng>([E]\d)?)";
                Regex _regex = new Regex(pattern);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(scriptFileName);
                MatchCollection mc = _regex.Matches(fileNameWithoutExtension);
                string[] model = new string[5];

                foreach (Match match in mc) {
                    GroupCollection gc = match.Groups;
                    model[0] = gc["model"].ToString();
                    model[1] = gc["mems_cmos_version"].ToString();
                    model[2] = gc["script_version"].ToString();
                    model[3] = gc["script_type"].ToString();
                    model[4] = gc["is_eng"].ToString();
                }
                testModel = model;
            } catch (Exception exception) {
                testModel = null;
                MessageBox.Show("Parsing product model process failed: \n" + exception.ToString());
                return;
            }
        }

        private void ReadTestScript(string scriptFileName, sType scriptType, out List<ScriptDefs> testParams, out List<string> testScriptErrors) {
            testParams = new List<ScriptDefs>();
            string scriptFileRead;
            List<string> scriptFileLines = new List<string>();
            List<string> scriptFileLinesWithLineNums = new List<string>();            
            testScriptErrors = new List<string>();

            List<string> formatErrors = new List<string>();
            List<string> binErrors = new List<string>();

            List<int> softwareBins = new List<int>();

            string[] scriptFileLineParams;
            try {
                // read file content including all delimiters
                StreamReader sr = new StreamReader(scriptFileName);
                scriptFileRead = sr.ReadToEnd();
                sr.Close();

                if (!scriptFileRead.Equals(string.Empty)) {
                    scriptFileLines.AddRange(scriptFileRead.Split(fileToLinesDelimiters, StringSplitOptions.None));
                    int lineNum = 1;
                    foreach (string line in scriptFileLines) {
                        scriptFileLinesWithLineNums.Add((lineNum++).ToString() + specialDelimeter[0] + line);
                    }

                    // first run the format and bin check, and also remove all comment lines
                    FormatCheck(ref scriptFileLinesWithLineNums, out softwareBins, out formatErrors);
                    if (formatErrors != null) {
                        testScriptErrors.AddRange(formatErrors);
                    }
                    tType testType = tType.UNKNOWN;
                    string testName = string.Empty;
                    string testUnit = string.Empty;
                    double testTarget = reservedBins[0];
                    double testSpecMin = reservedBins[0];
                    double testSpecMax = reservedBins[0];
                    int testSWBin = reservedBins[0];
                    int testHWBin = reservedBins[0];
                    for (int i = 0; i < scriptFileLinesWithLineNums.Count; i++) {
                        string[] thisLine = scriptFileLinesWithLineNums[i].Split(specialDelimeter, StringSplitOptions.None);
                        lineNum = Convert.ToInt16(thisLine[0].Trim());
                        if (lineNum == 1) {
                            //the display line was checked already in FormatCheck()
                            continue; 
                        }
                        scriptFileLineParams = thisLine[1].Split(lineToParamsDelimiters, StringSplitOptions.None);
                        int length = scriptFileLineParams.Length;
                        if (scriptFileLineParams[0].Trim().Equals("1")) {
                            string[] testParamPerLine = new string[8];
                            for (int j = 0; j < 12; j++) {
                                // testType, testName, testUnit, testTarget, testSpecMin, testSpecMax, testSWBin, testHWBin
                                if (j == 2 && length > 2) testType = ParseTestType(scriptFileLineParams[2].Trim().ToUpper());
                                if (j == 3 && length > 3) testName = scriptFileLineParams[3].Trim().ToUpper();
                                if (j == 4 && length > 4) testUnit = scriptFileLineParams[4].Trim().ToUpper();
                                if (j == 7 && length > 7) testTarget = ParseTarget(scriptFileLineParams[7]);
                                if (j == 8 && length > 8) testSpecMin = ParseTarget(scriptFileLineParams[8]);
                                if (j == 9 && length > 9) testSpecMax = ParseTarget(scriptFileLineParams[9]);
                                if (j == 10 && length > 10) testSWBin = ParseBins(scriptFileLineParams[10]);
                                if (j == 11 && length > 11) testHWBin = ParseBins(scriptFileLineParams[11]); 
                            }
                            testParams.Add(new ScriptDefs {
                                ScriptType = scriptType,
                                TestNum = lineNum,
                                TestType = testType,
                                TestName = testName,
                                TestUnit = testUnit,
                                TestTarget = testTarget,
                                TestSpecMin = testSpecMin,
                                TestSpecMax = testSpecMax,
                                TestSWBin = testSWBin,
                                TestHWBin = testHWBin
                            });
                        }
                    } // for loop of each line scanning

                    // more BIN checks
                    BinCheck(testParams, softwareBins, out binErrors);
                    if (binErrors != null) {
                        testScriptErrors.AddRange(binErrors);
                    }
                }
            } catch (Exception exception) {
                MessageBox.Show("Reading test script process failed: \n" + exception.ToString());
                return;
            }
        }

        private sType ParseScriptType(string[] testModel) {
            sType thisScriptType;
            string scriptType = testModel[3].Trim();
            if (scriptType == null || scriptType.Equals(string.Empty)) {
                scriptType = testModel[2].Substring(0, 2);
            } else {
                scriptType = scriptType.Substring(0, 2);
            }
            switch (scriptType.Trim().ToUpper()) {
                case "WS":
                    thisScriptType = sType.WS;
                    break;
                case "FV":
                case "FT":
                    thisScriptType = sType.FT;
                    break;
                case "WT":
                    thisScriptType = sType.WT;
                    break;
                case "QT":
                case "QV":
                    thisScriptType = sType.QV;
                    break;
                default:
                    thisScriptType = sType.UNKNOWN;
                    break;
            }

            return thisScriptType;
        }

        // BIN codes:
        // -999: format-that-can-not-be-parsed (not numeric)
        // -998: empty
        private int ParseBins(string param) {
            int result;
            if (param.Equals(string.Empty)) {
                //empty result -998
                result = reservedBins[1];
            }
            else {
               bool isInt = Int32.TryParse(param.Trim().ToUpper(), out result);
               if (isInt) {
               } else {
                   //anything else that can not be parsed is saved as -999
                   result = reservedBins[0];
               }
            }
            return result;
        }

        private double ParseTarget(string targetString) {
            // test target format has some variants. Here are some examples
            // set serialmode    I2C
            // check azstuck    20000|1.5|3|0.25
            // todo for proper checks on these types of target
            double testTarget;
            if (targetString.Equals(string.Empty)) testTarget = reservedBins[0];
            else {
                string targetString2 = targetString.Trim().ToUpper();
                bool isDouble = double.TryParse(targetString2, out testTarget);
                if (isDouble) {
                } else if (targetString2.Equals("I2C")) {
                    testTarget = reservedBins[1];
                } else if (targetString2.Equals("SPI")) {
                    testTarget = reservedBins[2];
                } else if (targetString2.Contains("|")) {
                    testTarget = reservedBins[3];
                } else {
                    testTarget = reservedBins[0];
                }
            }
            return testTarget;
        }
        private tType ParseTestType(string testType) {
            tType type;
            switch (testType) {
                case "SET":
                    type = tType.SET;
                    break;
                case "CHECK":
                    type = tType.CHECK;
                    break;
                case "TRIM":
                    type = tType.TRIM;
                    break;
                case "SWEEP":
                    type = tType.SWEEP;
                    break;
                default:
                    type = tType.UNKNOWN;
                    break;
            }
            return type;
        }
        
        private void FormatCheck(ref List<string> scriptLinesWithNums, out List<int> softwareBinsNoDups, out List<string> errors) {
            errors = new List<string>();
            List<string> newScriptLines = new List<string>();
            string[] lineDelimiters = new[] { "\t", "," };
            int counts = scriptLinesWithNums.Count;
            int maxBins = counts;
            softwareBinsNoDups = new List<int>();
            List<int> softwareBins = new List<int>();
            for (int i = 0; i < counts; i++) {
                string[] thisLine = scriptLinesWithNums[i].Split(specialDelimeter, StringSplitOptions.None);
                int lineNum = Convert.ToInt16(thisLine[0].Trim());
                // format rule 0: no empty line
                if (thisLine[1] == null || thisLine[1].Equals(String.Empty)) {
                    errors.Add("Line " + lineNum.ToString() + ": empty line.");
                    continue;
                }
                // format rule 1: script must begin with "display" word
                if (lineNum == 1 && !(thisLine[1].Trim().Substring(0, 7).ToUpper().Equals("DISPLAY"))) {
                    errors.Add("Script must begin with \"display\" word on the first line.");
                    continue;
                }
                // format rule 2: the first char of each line can not be space/tab
                string leadingChar = thisLine[1].Substring(0, 1);
                if (leadingChar.Equals(" ") || leadingChar.Equals("\t")) {
                    errors.Add("Line " + lineNum.ToString() + ": the first char of each line can not be space/tab.");
                    continue;
                }
                // skip commenting lines
                // Current "rules" for commenting in the script file:
                //   - line starting with "'" usually comments itself
                //   - multiple comments usually start with "*"
                // question: what about "="?
                if (leadingChar == "'" || thisLine[1].Contains("*") || thisLine[1].Contains("=")) {
                    continue;
                }
                //save software bins for next check                                
                string pattern = @"\d\s*(?<binNum>\d+)\s*set\s*bin";
                Regex _regex = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection mc = _regex.Matches(thisLine[1]);
                if (mc.Count == 0) {
                    newScriptLines.Add(thisLine[0] + specialDelimeter[0] + thisLine[1]);
                } else {
                    foreach (Match match in mc) {
                        GroupCollection gc = match.Groups;
                        int k = Convert.ToInt16(gc["binNum"].ToString());
                        if (k > 0) softwareBins.Add(k);
                    }
                }
            }
            // bin rule 1: all bin numbers should be unique (no dups)
            var duplicates = softwareBins.GroupBy(x => x)
                                .Where(x => x.Count() > 1)
                                .Select(x => x.Key)
                                .ToList();
            if (duplicates.Count > 1) {
                for (int i = 0; i < duplicates.Count; i++) {
                    errors.Add("Software BIN " + duplicates[i] + " is defined by multiple times");
                }
            }
            
            //remove dups
            softwareBinsNoDups = softwareBins.Distinct().ToList<int>();
            scriptLinesWithNums = newScriptLines;
        }

        private void BinCheck(List<ScriptDefs> testParams, List<int> softwareBins, out List<string> errors) {
            errors = new List<string>();
            int hwBinMin = 1;
            int hwBinMax = 5;
            bool isReserved = false;
            bool isReserved2 = false;
            for (int i = 0; i < testParams.Count; i++) {
                // hw bin should be in the range of [1-5]
                ScriptDefs pTest = testParams[i];
                for (int k = 0; k < reservedBins.GetLength(0); k++) {
                    if (pTest.TestHWBin == reservedBins[k]) {
                        isReserved = true;
                    }
                }
                if (!isReserved) { 
                    if (pTest.TestHWBin < hwBinMin || pTest.TestHWBin > hwBinMax) {
                        errors.Add("Line " + pTest.TestNum + ": hardware BIN out-of-range[1-5]");
                    }
                }

                // sw BIN checks         
                // 1. check if sw BIN is defined in the header
                isReserved = false;
                for (int k = 0; k < reservedBins.GetLength(0); k++) {
                    if (pTest.TestSWBin == reservedBins[k]) {
                        isReserved = true;
                    }
                }
                bool isMatch = false;
                if (!isReserved) {
                    for (int k = 0; k < softwareBins.Count; k++) {
                        if (pTest.TestSWBin == softwareBins[k]) isMatch = true;
                    }
                    if (!isMatch) {
                        errors.Add("Line " + pTest.TestNum + ": software BIN is not-defined");
                    }
                }

                // 2. limit is set, but sw/hw BIN empty (exception: for WS script, hw bin can be empty with limit set)
                if (pTest.ScriptType != sType.WS) {
                    isReserved = false;
                    isReserved2 = false;
                    for (int k = 0; k < reservedBins.GetLength(0); k++) {
                        if (pTest.TestSpecMin == reservedBins[k]) {
                            isReserved = true;
                        }
                        if (pTest.TestSpecMax == reservedBins[k]) {
                            isReserved2 = true;
                        }
                    }
                    if (!isReserved || !isReserved2) {
                        if (pTest.TestSWBin == reservedBins[1]) {
                            errors.Add("Line " + pTest.TestNum + ": limit is set, but software BIN empty");
                        }
                        if (pTest.TestHWBin == reservedBins[1]) {
                            errors.Add("Line " + pTest.TestNum + ": limit is set, but hardware BIN empty");
                        }

                    }
                }

                // 3. same sw BIN but with different hw BINs
                if (pTest.TestSWBin != reservedBins[1]) {
                    for (int j = i + 1; j < testParams.Count; j++) {
                        if (pTest.TestSWBin == testParams[j].TestSWBin) {
                            if (pTest.TestHWBin != testParams[j].TestHWBin && testParams[j].TestHWBin != reservedBins[1]) {
                                errors.Add("Lines " + pTest.TestNum + " and " + testParams[j].TestNum + ": same sw bin but with different hw bins");
                            }
                        }
                    }
                }

            }
        }





    }
}
