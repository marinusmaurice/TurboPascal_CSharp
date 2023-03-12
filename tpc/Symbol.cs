using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace tpc
{
    // Stores information about a single symbol.

    /**
     * Create a new symbol.
     *
     * name: name of symbol (original case is fine).
     * type: type of the symbol (Node.SIMPLE_TYPE, etc.).
     * address:
     *     if variable: address of symbol relative to mark pointer.
     *     if user procedure: address in istore.
     *     if system procedure: index into native array.
     * isNative: true if it's a native subprogram.
     * value: node of value if it's a constant.
     * byReference: whether this symbol is a reference or a value. This only applies
     *     to function/procedure parameters.
     */
    internal class Symbol
    {
        public bool isNative;
        public Node value;
        public bool byReference;
        public string name;
        public Node type;
        public int address;

        public Symbol(string name, Node type, int address, bool byReference)
        {
            this.name = name;
            this.type = type;
            this.address = address;
            this.isNative = false;
            this.value = null;
            this.byReference = byReference;
        }



    }
}
