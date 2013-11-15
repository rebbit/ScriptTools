using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptTools
{
    
    class Product
    {
        public FamilyName deviceFamily { get; private set; }
        public ProductName product { get; private set; }
        public byte swWhoAmI { get; private set; }
        public int continuity { get; private set; }
        public Product(FamilyName family, ProductName prod, byte swwai = 0x00, int c = 2047)
        {
            this.deviceFamily = family;
            this.product = prod;
            this.swWhoAmI = swwai;
            this.continuity = c;
        }

    }
}
