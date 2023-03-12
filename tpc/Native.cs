using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace tpc
{
    // Tracks a list of native functions that can be called from Pascal.
    internal class Native
    {
        private List<NativeProcedure> nativeProcedures;

        public Native()
        {
            // List of NativeProcedure objects. The index within the array is the
            // number passed to the "CSP" instruction.
            this.nativeProcedures = new List<NativeProcedure>();
        }

        // Adds a native method, returning its index.
        public int add(NativeProcedure nativeProcedure)
        {
            var index = this.nativeProcedures.Count;
            this.nativeProcedures.Add(nativeProcedure);
            return index;
        }

        // Get a native method by index.
        public NativeProcedure get(int index)
        {
            return this.nativeProcedures[index];
        }
    }
}
