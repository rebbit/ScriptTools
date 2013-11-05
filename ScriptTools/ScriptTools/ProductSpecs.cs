using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptTools
{
    class ProductSpecs
    {
        public string DeviceFamily { get; private set; }
        public string Product { get; private set; }
        public byte SWWhoAmI { get; private set; }
        public int Continuity { get; private set; }
        public ProductSpecs(string devicefamily, string product, byte swWhoAmI = 0x00, int continuity = 2047)
        {
            this.DeviceFamily = devicefamily;
            this.Product = product;
            this.SWWhoAmI = swWhoAmI;
            this.Continuity = continuity;
        }

    }
}
