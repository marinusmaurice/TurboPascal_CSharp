using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace tpc
{
    // Instruction set of p-machine. This machine language is compatible with the
    // p-code of 1978 UCSD Pascal.
    // 
    // References:
    //     http://cs2.uco.edu/~trt/cs4173/pspec.pdf
    //     http://cs2.uco.edu/~trt/cs4933/P-MachineSimulator.pdf
    internal class inst
    {


        //  define(["PascalError"], function (PascalError) {
        private static int OPCODE_BITS = 8;
        private static int OPERAND1_BITS = 9;
        private static int OPERAND2_BITS = 15;
        private static int OPCODE_MASK = (1 << OPCODE_BITS) - 1;
        private static int OPERAND1_MASK = (1 << OPERAND1_BITS) - 1;
        private static int OPERAND2_MASK = (1 << OPERAND2_BITS) - 1;
        private static int OPCODE_SHIFT = 0;
        private static int OPERAND1_SHIFT = OPCODE_SHIFT + OPCODE_BITS;
        private static int OPERAND2_SHIFT = OPERAND1_SHIFT + OPERAND1_BITS;

        public static class defs
        {
            // Op codes.            Description                  operand1        operand2
            // Subprogram linkage.
            public const int CUP = 0x00;      //      Call user procedure          argsize         iaddr
            public const int CSP = 0x01;      //      Call standard procedure      argsize         stdfunction
            public const int ENT = 0x02;      //      Entry                        register        amount
            public const int MST = 0x03;      //      Mark stack                   level
            public const int RTN = 0x04;      //      Return                       type
                                               // Comparison.
            public const int EQU = 0x05;      //      Equality                     type
            public const int NEQ = 0x06;      //      Inequality                   type
            public const int GRT = 0x07;      //      Greater than                 type
            public const int GEQ = 0x08;      //      Greater than or equal        type
            public const int LES = 0x09;      //      Less than                    type
            public const int LEQ = 0x0A;      //      Less than or equal           type
                                               // Integer arithmetic.
            public const int ADI = 0x0B;      //      Integer addition
            public const int SBI = 0x0C;      //      Integer subtraction
            public const int NGI = 0x0D;      //      Integer sign inversion
            public const int MPI = 0x0E;      //      Integer multiplication
            public const int DVI = 0x0F;      //      Integer division
            public const int MOD = 0x10;      //      Integer modulo
            public const int ABI = 0x11;      //      Integer absolute value
            public const int SQI = 0x12;      //      Integer square
            public const int INC = 0x13;      //      Integer increment            i-type
            public const int DEC = 0x14;      //      Integer decrement            i-type
                                               // Real arithmetic.     
            public const int ADR = 0x15;      //      Real addition
            public const int SBR = 0x16;      //      Real subtraction
            public const int NGR = 0x17;      //      Real sign inversion
            public const int MPR = 0x18;      //      Real multiplication
            public const int DVR = 0x19;      //      Real division
            public const int ABR = 0x1A;      //      Real absolute value
            public const int SQR = 0x1B;      //      Real square
                                               // Boolean.
            public const int IOR = 0x1C;      //      Inclusive OR.
            public const int AND = 0x1D;      //      AND
            public const int XOR = 0x1E;      //      Exclusive OR.
            public const int NOT = 0x1F;      //      NOT.
                                               // Set operations.
            public const int INN = 0x20;      //      Set membership.
            public const int UNI = 0x21;      //      Set union.
            public const int INT = 0x22;      //      Set intersection.
            public const int DIF = 0x23;      //      Set difference.
            public const int CMP = 0x24;      //      Set complement.
            public const int SGS = 0x25;      //      Generate singleton set.
                                               // Jump.
            public const int UJP = 0x26;      //      Unconditional jump.                          iaddr
            public const int XJP = 0x27;      //      Indexed jump.                                iaddr
            public const int FJP = 0x28;      //      False jump.                                  iaddr
            public const int TJP = 0x29;      //      True jump.                                   iaddr
                                               // Conversion.  
            public const int FLT = 0x2A;      //      Integer to real.
            public const int FLO = 0x2B;      //      Integer to real (2nd entry on stack).
            public const int TRC = 0x2C;      //      Truncate.
            public const int RND = 0x2C;      //      Round.
            public const int CHR = 0x2C;      //      Integer to char.
            public const int ORD = 0x2C;      //      Anything to integer.
                                               // Termination.
            public const int STP = 0x30;      //      Stop.
                                               // Data reference.
            public const int LDA = 0x31;      //      Load address of data         level           offset
            public const int LDC = 0x32;      //      Load constant                type            cindex
            public const int LDI = 0x33;      //      Load indirect                type
            public const int LVA = 0x34;      //      Load value (address)         level           offset
            public const int LVB = 0x35;      //      Load value (boolean)         level           offset
            public const int LVC = 0x36;      //      Load value (character)       level           offset
            public const int LVI = 0x37;      //      Load value (integer)         level           offset
            public const int LVR = 0x38;      //      Load value (real)            level           offset
            public const int LVS = 0x39;      //      Load value (set)             level           offset
            public const int STI = 0x3A;      //      Store indirect               type
            public const int IXA = 0x3B;      //      Compute indexed address                      stride

            // Registers.
            public const int REG_SP = 0x00;   //      Stack pointer.
            public const int REG_EP = 0x01;   //      Extreme pointer (not used in this machine).
            public const int REG_MP = 0x02;   //      Mark pointer.
            public const int REG_PC = 0x03;   //      Program counter.
            public const int REG_NP = 0x04;   //      New pointer.

            // Types.
            public const int A = 0x00;        //      Address.
            public const int B = 0x01;        //      Boolean.
            public const int C = 0x02;        //      Character.
            public const int I = 0x03;        //      Integer.
            public const int R = 0x04;        //      Real.
            public const int S = 0x05;        //      String.
            public const int T = 0x06;        //      Set.
            public const int P = 0x07;        //      Procedure (aka void; returned by procedure).
            public const int X = 0x08;        //      Any.

            // The Mark is the area at the bottom of each frame. It contains (low to high address):
            //
            //     Return value (rv).
            //     Static link (sl).
            //     Dynamic link (dl).
            //     Extreme pointer (es), not used.
            //     Return address (ra).
            //
            public static int MARK_SIZE = 5;

            // Opcode number (such as 0x32) to name ("LDC").
            // Populated procedurally below.
            public static Dictionary<int, string> opcodeToName = new Dictionary<int, string>();

            // Construct a machine language instruction.
            public static int make(int opcode, int operand1, int operand2)
            {
                  // Allow caller to leave out these operands.
                operand1 = operand1 ?? 0;
                operand2 = operand2 ?? 0;

                // Sanity check.
                if (operand1 < 0)
                {
                    throw new PascalError(null, "negative operand1: " + operand1);
                }
                if (operand1 > OPERAND1_MASK)
                {
                    throw new PascalError(null, "too large operand1: " + operand1);
                }
                if (operand2 < 0)
                {
                    throw new PascalError(null, "negative operand2: " + operand2);
                }
                if (operand2 > OPERAND2_MASK)
                {
                    throw new PascalError(null, "too large operand2: " + operand2);
                }

                return (opcode << OPCODE_SHIFT) |
                    (operand1 << OPERAND1_SHIFT) |
                    (operand2 << OPERAND2_SHIFT);
            }

            // Return the opcode of the instruction.
            public static int getOpcode(int i)
            {
                return (i >>> OPCODE_SHIFT) & OPCODE_MASK;
            }

            // Return operand 1 of the instruction.
            public static int getOperand1(int i)
            {
                return (i >>> OPERAND1_SHIFT) & OPERAND1_MASK;
            }

            // Return operand 2 of the instruction.
            public static int getOperand2(int i)
            {
                return (i >>> OPERAND2_SHIFT) & OPERAND2_MASK;
            }

            // Return a string version of the instruction.
            public static string disassemble(int i)
            {
                var opcode = getOpcode(i);
                var operand1 = getOperand1(i);
                var operand2 = getOperand2(i);

                return opcodeToName[opcode] + " " + operand1 + " " + operand2;
            }

            // Converts a type code like inst.I to "integer", or throw if not valid.
            public static string typeCodeToName(int typeCode)
            {
                switch (typeCode)
                {
                    case A:
                        return "pointer";
                    case B:
                        return "boolean";
                    case C:
                        return "char";
                    case I:
                        return "integer";
                    case R:
                        return "real";
                    case S:
                        return "string";
                    default:
                        throw new PascalError(null, "unknown type code " + typeCode);
                }
            }
        }

        // Make an inverse table of opcodes.
        public inst()
        {
            defs.opcodeToName[defs.CUP] = "CUP";
            defs.opcodeToName[defs.CSP] = "CSP";
            defs.opcodeToName[defs.ENT] = "ENT";
            defs.opcodeToName[defs.MST] = "MST";
            defs.opcodeToName[defs.RTN] = "RTN";
            defs.opcodeToName[defs.EQU] = "EQU";
            defs.opcodeToName[defs.NEQ] = "NEQ";
            defs.opcodeToName[defs.GRT] = "GRT";
            defs.opcodeToName[defs.GEQ] = "GEQ";
            defs.opcodeToName[defs.LES] = "LES";
            defs.opcodeToName[defs.LEQ] = "LEQ";
            defs.opcodeToName[defs.ADI] = "ADI";
            defs.opcodeToName[defs.SBI] = "SBI";
            defs.opcodeToName[defs.NGI] = "NGI";
            defs.opcodeToName[defs.MPI] = "MPI";
            defs.opcodeToName[defs.DVI] = "DVI";
            defs.opcodeToName[defs.MOD] = "MOD";
            defs.opcodeToName[defs.ABI] = "ABI";
            defs.opcodeToName[defs.SQI] = "SQI";
            defs.opcodeToName[defs.INC] = "INC";
            defs.opcodeToName[defs.DEC] = "DEC";
            defs.opcodeToName[defs.ADR] = "ADR";
            defs.opcodeToName[defs.SBR] = "SBR";
            defs.opcodeToName[defs.NGR] = "NGR";
            defs.opcodeToName[defs.MPR] = "MPR";
            defs.opcodeToName[defs.DVR] = "DVR";
            defs.opcodeToName[defs.ABR] = "ABR";
            defs.opcodeToName[defs.SQR] = "SQR";
            defs.opcodeToName[defs.IOR] = "IOR";
            defs.opcodeToName[defs.AND] = "AND";
            defs.opcodeToName[defs.XOR] = "XOR";
            defs.opcodeToName[defs.NOT] = "NOT";
            defs.opcodeToName[defs.INN] = "INN";
            defs.opcodeToName[defs.UNI] = "UNI";
            defs.opcodeToName[defs.INT] = "INT";
            defs.opcodeToName[defs.DIF] = "DIF";
            defs.opcodeToName[defs.CMP] = "CMP";
            defs.opcodeToName[defs.SGS] = "SGS";
            defs.opcodeToName[defs.UJP] = "UJP";
            defs.opcodeToName[defs.XJP] = "XJP";
            defs.opcodeToName[defs.FJP] = "FJP";
            defs.opcodeToName[defs.TJP] = "TJP";
            defs.opcodeToName[defs.FLT] = "FLT";
            defs.opcodeToName[defs.FLO] = "FLO";
            defs.opcodeToName[defs.TRC] = "TRC";
            defs.opcodeToName[defs.RND] = "RND";
            defs.opcodeToName[defs.CHR] = "CHR";
            defs.opcodeToName[defs.ORD] = "ORD";
            defs.opcodeToName[defs.STP] = "STP";
            defs.opcodeToName[defs.LDA] = "LDA";
            defs.opcodeToName[defs.LDC] = "LDC";
            defs.opcodeToName[defs.LDI] = "LDI";
            defs.opcodeToName[defs.LVA] = "LVA";
            defs.opcodeToName[defs.LVB] = "LVB";
            defs.opcodeToName[defs.LVC] = "LVC";
            defs.opcodeToName[defs.LVI] = "LVI";
            defs.opcodeToName[defs.LVR] = "LVR";
            defs.opcodeToName[defs.LVS] = "LVS";
            defs.opcodeToName[defs.STI] = "STI";
            defs.opcodeToName[defs.IXA] = "IXA";
        }
        
        

    }
}
