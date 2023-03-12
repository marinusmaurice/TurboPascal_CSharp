using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace tpc
{
    // The result of a symbol lookup.
    internal class SymbolLookup
    {
        public Symbol symbol;
        public int level;

        public SymbolLookup(Symbol symbol, int level)
        {
            // The symbol found.
            this.symbol = symbol;

            // The number of levels that had to be searched. Zero means it was
            // found in the innermost level.
            this.level = level;
        }
    }
}
