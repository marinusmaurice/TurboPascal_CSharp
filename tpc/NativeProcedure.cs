using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace tpc
{
    // The object that's stored in the Native store.
    internal class NativeProcedure
    {
        public string name;
        public Node returnType;
        public List<Node> parameterTypes;
        public object fn;
         
        // Object that's stored in the Native array.
        public NativeProcedure(string name, Node returnType, List<Node> parameterTypes, object fn)
        {
            this.name = name;
            this.returnType = returnType;
            this.parameterTypes = parameterTypes;
            this.fn = fn;
        }

    }
}
