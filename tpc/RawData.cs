using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace tpc
{
    // An object that stores a linear array of raw data (constants) and a parallel
    // array of their simple type codes.
    internal class RawData
    {
        internal int length;
        internal List<object> data;
        internal List<int> simpleTypeCodes;

        public RawData() {
                this.length = 0;
                this.data = new List<object>();
                this.simpleTypeCodes = new List<int>();
            }

            // Adds a piece of data and its simple type (inst.I, etc.) to the list.
            public void add(object datum, int simpleTypeCode) {
                this.length++;
                this.data.Add(datum);
                this.simpleTypeCodes.Add(simpleTypeCode);
            }

            // Adds a SIMPLE_TYPE node.
            public void addNode(Node node) {
                this.add(node.getConstantValue(), node.expressionType.getSimpleTypeCode());
            }

            // Print the array for human debugging.
            public string print() {
                return "(" + string.Join(", ", data.ToArray()) + ")";
            }

        

    }
}
