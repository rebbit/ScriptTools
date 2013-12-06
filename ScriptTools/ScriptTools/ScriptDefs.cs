using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptTools {
    //script type
    enum sType {
        UNKNOWN,
        WS,
        FT,
        WT,
        QV
    }
    //test type
    enum tType {
        UNKNOWN,
        SET,
        CHECK,
        TRIM,
        SWEEP
    }
    enum serialMode {
        I2C,
        SPI
    }
    class ScriptDefs {
        public sType ScriptType { get; set; }
        public int TestNum { get; set; } //this is for record purpose
        // testType, testName, testUnit, testTarget, testSpecMin, testSpecMax, testSWBin, testHWBin
        public tType TestType { get; set; }
        public string TestName { get; set; }
        public string TestUnit { get; set; }
        public double TestTarget { get; set; }
        public double TestSpecMin { get; set; }
        public double TestSpecMax { get; set; }
        public int TestSWBin { get; set; }
        public int TestHWBin { get; set; }
    }
}
