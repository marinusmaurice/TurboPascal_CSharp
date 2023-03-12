using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace tpc
{
    // The bytecode object. Stores all bytecodes for a program, along with accompanying
    // data (such as program constants).
    internal class ByteCode
    {
        public List<double> istore;
        public List<object> constants;
        public List<object> typedConstants;
        public int startAddress;
        private Dictionary<int,string> comments;
        public Native native;

        public ByteCode(Native native)
        {
            // Instructions. Array of doubles.
            this.istore = new List<double>();

            // Constants. This is an ordered list of JavaScript constant objects, such
            // as numbers and strings.
            this.constants = new List<object>();

            // Typed constants. These are copied to the start of the dstore when
            // the bytecode is loaded.
            this.typedConstants = new List<object>();

            // Index into istore where program should start.
            this.startAddress = 0;

            // Map from istore address to comment.
            this.comments = new Dictionary<int, string>();

            // Native methods.
            this.native = native;
        }

        // Add a constant (of any type), returning the cindex.
        public int addConstant(object c)
        {
            // Re-use existing constants. We could use a hash table for this.
            for (var i = 0; i < this.constants.Count; i++)
            {
                if (c == this.constants[i])
                {
                    return i;
                }
            }

            // Add new constants.
            this.constants.Add(c);
            return this.constants.Count - 1;
        }

        // Add an array of words to the end of the typed constants. Returns the
        // address of the item that was just added.
        public int addTypedConstants(object raw)
        {
            var address = this.typedConstants.Count;

            // Append entire "raw" array to the back of the typedConstants array.
            //TODO: MVM this.typedConstants.push.apply(this.typedConstants, raw);  //TODO: MVM

            return address;
        }

        // Add an opcode to the istore.
        public void add(int opcode,int operand1,int operand2, string comment)
        {
            var i = inst.defs.make(opcode, operand1, operand2);
            var address = this.getNextAddress();
            this.istore.Add(i);
            if (comment != null)
            {
                this.addComment(address, comment);
            }
        }

        // Replace operand2 of the instruction.
        public void setOperand2(int address,int operand2)
        {
            var i = this.istore[address];
            i = inst.defs.make(inst.defs.getOpcode((int)i), inst.defs.getOperand1((int)i), operand2);
            this.istore[address] = i;
        }

        // Return the next address to be added to the istore.
        public int getNextAddress()
        {
            return this.istore.Count;
        }

        // Return a printable version of the bytecode object.
        public string print()
        {
            return this._printConstants() + "\n" + this._printIstore();
        }

        // Set the starting address to the next instruction that will be added.
        public void setStartAddress()
        {
            this.startAddress = this.getNextAddress();
        }

        // Add a comment to the address.
        public void addComment(int address,string comment)
        {
            var existingComment = this.comments[address];
            if (existingComment != null)
            {
                // Add to existing comment.
                comment = existingComment + "; " + comment;
            }
            this.comments[address] = comment;
        }

        // Return a printable version of the constant table.
        public string _printConstants()
        {
            List<string> lines = new List<string>();
            for (int i = 0; i < this.constants.Count; i++)
            {
                var value = this.constants[i];
                if (value.GetType() == typeof(string))
                {
                    value = "'" + value + "'";
                }
                lines.Add(utils.rightAlign(i.ToString(), 4) + ": " + value);
            }

            return "Constants:\n" + string.Join("\n", lines.ToArray()) + "\n";
        }

        // Return a printable version of the istore array.
        public string _printIstore()
        {
            List<string> lines = new List<string>();
            for (int address = 0; address < this.istore.Count; address++)
            {
                var line = utils.rightAlign(address.ToString(), 4) + ": " +
                    utils.leftAlign(inst.defs.disassemble((int)this.istore[address]), 11);
                var comment = this.comments[address];
                if (comment != null)
                {
                    line += " ; " + comment;
                }
                lines.Add(line);
            }

            return "Istore:\n" + string.Join("\n", lines.ToArray()) + "\n";
        } 
    }
}
