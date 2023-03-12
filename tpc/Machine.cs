using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace tpc
{
    // Virtual p-machine (pseudo-machine) for bytecode.
    internal class Machine
    {
        private ByteCode bytecode;
        private Keyboard keyboard;
        private long startTime;
        private int pc;
        private int sp;
        private int mp;
        private int np;
        private int ep;
        private int state;
        private Action<string> debugCallback;
        private Action<long> finishCallback;
        private Action<string> outputCallback;
        private Action<string> inputCallback;
        private int[] dstore = new int[65536];
        private int pendingDelay;
        private int undefined = Int32.MinValue;
        public Control control;

        public Machine(ByteCode bytecode, Keyboard keyboard)
        {
            this.bytecode = bytecode;
            this.keyboard = keyboard;

            // Time that the program was started, in ms since epoch.
            this.startTime = 0;

            // Data store. Used for the stack, which grows up from address 0.
            this.dstore = new int[65536];

            // Program counter. Points into the istore of the bytecode.
            this.pc = 0;

            // Stack Pointer. Points into the dstore. The specifications for the
            // p-machine say that SP points to the top-most item on the stack (the
            // item most recently pushed), but here we point one past that. I'm too
            // used to the latter convention and it would cause too many bugs for
            // me to switch. Besides, other docs imply that the p-machine used my
            // convention anyway, so I can't be sure.
            this.sp = 0;

            // Mark Pointer. Points into the dstore. Points to the bottom of the
            // stack frame.
            this.mp = 0;

            // New Pointer. Points into the dstore. Points to the bottom of the heap,
            // the lowest address within the heap.
            this.np = 0;

            // Extreme Pointer. Points to the highest stack address used by the
            // currently-executing procedure. This is an optimization so that
            // we only need to check in one place (when EP is increased) whether
            // we've crashed into the New Pointer. We don't use this.
            this.ep = 0;

            // The state of the machine (STATE_...).
            this.state = STATE_STOPPED;

            // Debug callback. Can be called with a string that should be displayed to
            // the user.
            this.debugCallback = null;

            // Finish callback. Called when the program terminates, either by running off
            // the end of the program's begin/end block, or by calling halt. The callback
            // is passed the number of seconds that the program ran.
            this.finishCallback = null;

            // Callback that standard output is sent to. This is called once per
            // line of output, and the line is the only parameter.
            this.outputCallback = null;

            // Callback that gets a line of input from the user. It is called with
            // a function that will be called with the line of input.
            this.inputCallback = null;

            // The number of ms that the program is expecting us to delay now.
            this.pendingDelay = 0;

            // Control object for native functions to manipulate this machine.
            var self = this;

        }

        public class Control
        {
            Machine self;
            public Control(Machine machine)
            {
                self = machine;
            }
            // Stop the machine.
            public void stop()
            {
                self.stopProgram();
            }
            // Suspend the machine (stop processing instructions).
            public void suspend()
            {
                self.state = Machine.STATE_SUSPENDED;
            }
            // Resume the machine (un-suspend).
            public void resume()
            {
                self.resume();
            }
            // Wait "ms" milliseconds.
            public void delay(int ms)
            {
                self.pendingDelay = ms;
            }
            // Write the line to the output.
            public void writeln(string line)
            {
                if (self.outputCallback != null)
                {
                    self.outputCallback(line);
                }
            }
            // Read a line from the user. The parameter is a function that
            // will be called with the line. The machine must first be suspended.
            public void readln(Action<string> callback)
            {
                if (self.inputCallback != null)
                {
                  //TODO: MVM  self.inputCallback(callback);
                }
                else
                {
                    callback("no input");
                }
            }
            // Read a value from memory.
            public int readDstore(int address)
            {
                return self.dstore[address];
            }
            // Write a value to memory.
            public void writeDstore(int address, int value)
            {
                self.dstore[address] = value;
            }
            // Push a value onto the stack.
            public void push(int value)
            {
                self._push(value);
            }
            // Allocate some memory from the heap.
            public int malloc(int size)
            {
                return self._malloc(size);
            }
            // Free some memory from the heap.
            public void free(int p)
            {
                //return self._free(p);
                self._free(p);
            }
            // Check whether a key has been pressed.
            public bool keyPressed()
            {
                if (self.keyboard != null)
                {
                    return self.keyboard.keyPressed();
                }
                else
                {
                    return false;
                }
            }
            // Read a key from the keyboard, or 0 for none.
            public int readKey()
            {
                if (self.keyboard != null)
                {
                    return self.keyboard.readKey();
                }
                else
                {
                    return 0;
                }
            }
        }

        // Various machine states.
        private const int STATE_STOPPED = 0;
        private const int STATE_RUNNING = 1;
        private const int STATE_SUSPENDED = 2;

        // Run the bytecode.
        public void run()
        {
            // Reset the machine.
            this._reset();

            // Start the machine.
            this.startTime = DateTime.Now.Ticks; //TODO: MVM new Date().getTime();

            this.resume();
        }

        // Continue running the program.
       public void resume()
        {
            // Run the program.
            this.state = Machine.STATE_RUNNING;
            this._dumpState();

            // Define a function that will run a finite number of instructions,
            // then temporarily return control to the browser for display update
            // and input processing.
            var self = this;

            //TODO: MVM
            ////////var stepAndTimeout = function() {
            ////////    self.step(100000);

            ////////    // If we're still running, schedule another brief run.
            ////////    if (self.state == Machine.STATE_RUNNING)
            ////////    {
            ////////        var delay = self.pendingDelay;
            ////////        self.pendingDelay = 0;
            ////////        setTimeout(stepAndTimeout, delay);
            ////////    }
            ////////};

            ////////// Kick it off.
            ////////stepAndTimeout();
        }

        // Step "count" instructions. Does nothing if the program is stopped.
        public void step(int count)
        {
            for (var i = 0; i < count && this.state == Machine.STATE_RUNNING &&
                 this.pendingDelay == 0; i++)
            {

                this.stepOnce();
            }
        }

        // Step one instruction. The machine *must* be running.
        public void stepOnce()
        {
            try
            {
                this._executeInstruction();
            }
            catch (Exception e)
            {
                if (e is PascalError) {
                    Console.WriteLine(((PascalError)e).getMessage());
                }
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(this._getState());
                this.stopProgram();
            }
            this._dumpState();
        }

        // Set a callback for debugging. The callback is called with a string that should
        // be displayed to the user.
        public void setDebugCallback(Action<string> debugCallback)
        {
            this.debugCallback = debugCallback;
        }

        // Set a callback for when the program ends. The callback is called with a number for
        // the number of seconds that the program ran.
        public void setFinishCallback(Action<long> finishCallback)
        {
            this.finishCallback = finishCallback;
        }

        // Set a callback for standard output. The callback is called with a string to
        // write.
        public void setOutputCallback(Action<string> outputCallback)
        {
            this.outputCallback = outputCallback;
        }

        // Set a callback for standard input. The callback is called with a function
        // that takes the line that was entered.
        public void setInputCallback(Action<string> inputCallback)
        {
            this.inputCallback = inputCallback;
        }

        // Dump the state of the machine to the debug callback.
        public void _dumpState()
        {
            if (this.debugCallback != null)
            {
                this.debugCallback(this._getState());
            }
        }

        // Generate a string which is a human-readable version of the machine state.
        public string _getState()
        {
            // Clip off stack display since it can be very large with arrays.
            var maxStack = 20;
            // Skip typed constants.
            var startStack = this.bytecode.typedConstants.Count;
            var clipStack = Math.Max(startStack, this.sp - maxStack);
            var stack = JSON.stringify(this.dstore.slice(clipStack, this.sp));
            if (clipStack > startStack)
            {
                // Trim stack.
                stack = stack[0] + "...," + stack.slice(1, stack.Length);
            }

            // Clip off heap display since it can be very large with arrays.
            var maxHeap = 20;
            var heapSize = this.dstore.Length - this.np;
            var heapDisplay = Math.Min(maxHeap, heapSize);
            var heap = JSON.stringify(this.dstore.slice(
                this.dstore.Length - heapDisplay, this.dstore.Length));
            if (heapDisplay != heapSize)
            {
                // Trim heap.
                heap = heap[0] + "...," + heap.slice(1, heap.Length);
            }

            var state = new List<string>() {
                "pc = " + utils.rightAlign(this.pc.ToString(), 4),
                utils.leftAlign(inst.defs.disassemble((int)this.bytecode.istore[this.pc]), 11),
                /// "sp = " + utils.rightAlign(this.sp, 3),
                "mp = " + utils.rightAlign(this.mp.ToString(), 3),
                "stack = " + utils.leftAlign(stack, 40),
                "heap = " + heap
            };

            return string.Join(" ", state.ToArray());
        }

        // Push a value onto the stack.
        public void _push(int value)
        {
            // Sanity check.
            if (value == null) //TODO: MVM || value == undefined)
            {
                throw new PascalError(null, "can't push " + value);
            }
            this.dstore[this.sp++] = value;
        }

        // Pop a value off the stack.
        public int _pop()
        {
            --this.sp;
            var value = this.dstore[this.sp];

            // Set it to undefined so we can find bugs more easily.
            this.dstore[this.sp] = undefined;

            return value;
        }

        // Reset the machines state.
        public void _reset()
        {
            // Copy the typed constants into the dstore.
            for (int i = 0; i < this.bytecode.typedConstants.Count; i++)
            {
                this.dstore[i] = (int)this.bytecode.typedConstants[i];
            }

            // The bytecode has a specific start address (the main block of the program).
            this.pc = this.bytecode.startAddress;
            this.sp = this.bytecode.typedConstants.Count;
            this.mp = 0;
            this.np = this.dstore.Length;
            this.ep = 0;
            this.state = Machine.STATE_STOPPED;
        }

        // Get the static link off the mark.
        public int _getStaticLink(int mp)
        {
            // The static link is the second entry in the mark.
            return this.dstore[mp + 1];
        }

        // Verifies that the data address is valid, meaning that it's in the
        // stack or the heap. Throws if not.
        public void _checkDataAddress(int address)
        {
            if (address >= this.sp && address < this.np)
            {
                throw new PascalError(null, "invalid data address (" +
                    this.sp + " <= " + address + " < " + this.np + ")");
            }
        }

        // If the program is running, stop it and called the finish callback.
        public void stopProgram()
        {
            if (this.state != STATE_STOPPED)
            {
                this.state = Machine.STATE_STOPPED;
                if (this.finishCallback != null)
                {
                    this.finishCallback((DateTime.Now.Ticks - this.startTime) / 1000);
                }
            }
        }

        // Execute the next instruction.
        public void _executeInstruction()
        {
            // Get this instruction.
            var pc = this.pc;
            var ix = (int)this.bytecode.istore[pc];

            // Advance the PC right away. Various instructions can then modify it.
            this.pc++;

            var opcode = inst.defs.getOpcode(ix);
            var operand1 = inst.defs.getOperand1(ix);
            var operand2 = inst.defs.getOperand2(ix);

            switch (opcode)
            {
                case inst.defs.CUP:
                    // Call User Procedure. By now SP already points past the mark
                    // and the parameters. So we set the new MP by backing off all
                    // those. Opcode1 is the number of parameters passed in.
                    this.mp = this.sp - operand1 - inst.defs.MARK_SIZE;

                    // Store the return address.
                    this.dstore[this.mp + 4] = this.pc;

                    // Jump to the procedure.
                    this.pc = operand2;
                    break;
                case inst.defs.CSP:
                    // Call System Procedure. We look up the index into the Native object
                    // and call it.
                    var nativeProcedure = this.bytecode.native.get(operand2);

                    // Pop parameters.
                    List<object> parameters = new List<object>();
                    for (int x = 0; x < operand1; x++)
                    {
                        // They are pushed on the stack first to last, so we
                        // unshift them (push them on the front) so they end up in
                        // the right order.
                        parameters.Insert(0, this._pop());
                    }

                    // Push the control object that the native function can use to
                    // control this machine.
                    parameters.Insert(0, this.control);

                    // Call the built-in function.
                    int returnValue = 0; //TODO: MVM nativeProcedure.fn.apply(null, parameters);

                    // See if we're still running. The function might have stopped
                    // or suspended us.
                    if (this.state == Machine.STATE_RUNNING)
                    {
                        // Push result if we're a function.
                        if (!nativeProcedure.returnType.isSimpleType(inst.defs.P))
                        {
                            this._push(returnValue);
                        }
                    }
                    break;
                case inst.defs.ENT:
                    // Entry. Set SP or EP to MP + operand2, which is the sum of
                    // the mark size, the parameters, and all local variables. If
                    // we're setting SP, then we're making room for local variables
                    // and preparing the SP to do computation.
                    var address = this.mp + operand2;
                    if (operand1 == 0)
                    {
                        // Clear the local variable area.
                        for (int x = this.sp; x < address; x++)
                        {
                            this.dstore[x] = 0;
                        }
                        this.sp = address;
                    }
                    else
                    {
                        this.ep = address;
                    }
                    break;
                case inst.defs.MST:
                    // Follow static links "operand1" times.
                    var sl = this.mp;
                    for (var i = 0; i < operand1; i++)
                    {
                        sl = this._getStaticLink(sl);
                    }

                    // Mark Stack.
                    this._push(0);              // RV, set by called function.
                    this._push(sl);             // SL
                    this._push(this.mp);        // DL
                    this._push(this.ep);        // EP
                    this._push(0);              // RA, set by CUP.
                    break;
                case inst.defs.RTN:
                    // Return.
                    var oldMp = this.mp;
                    this.mp = this.dstore[oldMp + 2];
                    this.ep = this.dstore[oldMp + 3];
                    this.pc = this.dstore[oldMp + 4];
                    if (operand1 == inst.defs.P)
                    {
                        // Procedure, pop off the return value.
                        this.sp = oldMp;
                    }
                    else
                    {
                        // Function, leave the return value on the stack.
                        this.sp = oldMp + 1;
                    }
                    break;
                case inst.defs.EQU:
                    // Equal To.
                    {
                        var op2EQ = this._pop();
                        var op1EQ = this._pop();
                        this._push(Convert.ToInt32(op1EQ == op2EQ));
                    }
                    break;
                case inst.defs.NEQ:
                    // Not Equal To.
                    {
                        var op2NEQ = this._pop();
                        var op1NEQ = this._pop();
                        this._push(Convert.ToInt32(op1NEQ != op2NEQ));
                    }
                    break;
                case inst.defs.GRT:
                    // Greater Than.
                    var op2GRT = this._pop();
                    var op1GRT = this._pop();
                    this._push(Convert.ToInt32(op1GRT > op2GRT));
                    break;
                case inst.defs.GEQ:
                    // Greater Than Or Equal To.
                    var op2GEQ = this._pop();
                    var op1GEQ = this._pop();
                    this._push(Convert.ToInt32(op1GEQ >= op2GEQ));
                    break;
                case inst.defs.LES:
                    // Less Than.
                    var op2LES = this._pop();
                    var op1LES = this._pop();
                    this._push(Convert.ToInt32(op1LES < op2LES));
                    break;
                case inst.defs.LEQ:
                    // Less Than Or Equal To.
                    var op2LEQ = this._pop();
                    var op1LEQ = this._pop();
                    this._push(Convert.ToInt32(op1LEQ <= op2LEQ));
                    break;
                case inst.defs.ADI:
                case inst.defs.ADR:
                    // Add integer/real.
                    var op2A = this._pop();
                    var op1A = this._pop();
                    this._push(op1A + op2A);
                    break;
                case inst.defs.SBI:
                case inst.defs.SBR:
                    // Subtract integer/real.
                    var op2S = this._pop();
                    var op1S = this._pop();
                    this._push(op1S - op2S);
                    break;
                case inst.defs.NGI:
                case inst.defs.NGR:
                    // Negate.
                    this._push(-this._pop());
                    break;
                case inst.defs.MPI:
                case inst.defs.MPR:
                    // Multiply integer/real.
                    var op2M = this._pop();
                    var op1M = this._pop();
                    this._push(op1M * op2M);
                    break;
                case inst.defs.DVI:
                    // Divide integer.
                    var op2DVI = this._pop();
                    var op1DVI = this._pop();
                    if (op2DVI == 0)
                    {
                        throw new PascalError(null, "divide by zero");
                    }
                    this._push(Convert.ToInt32((utils.trunc(op1DVI / op2DVI))));  //TODO: MVM
                    break;
                case inst.defs.MOD:
                    // Modulo.
                    var op2MOD = this._pop();
                    var op1MOD = this._pop();
                    if (op2MOD == 0)
                    {
                        throw new PascalError(null, "modulo by zero");
                    }
                    this._push(op1MOD % op2MOD);
                    break;
                // case inst.defs.ABI:
                // case inst.defs.SQI:
                case inst.defs.INC:
                    // Increment.
                    this._push(this._pop() + 1);
                    break;
                case inst.defs.DEC:
                    // Decrement.
                    this._push(this._pop() - 1);
                    break;
                case inst.defs.DVR:
                    // Divide real.
                    var op2DVR = this._pop();
                    var op1DVR = this._pop();
                    if (op2DVR == 0)
                    {
                        throw new PascalError(null, "divide by zero");
                    }
                    this._push(op1DVR / op2DVR);
                    break;
                // case inst.defs.ABR:
                // case inst.defs.SQR:
                case inst.defs.IOR:
                    // Inclusive OR.
                    var op2IOR = this._pop();
                    var op1IOR = this._pop();
                    //TODO: MVM this._push(op1IOR || op2IOR);
                    break;
                case inst.defs.AND:
                    // AND
                    var op2AND = this._pop();
                    var op1AND = this._pop();
                    //TODO: MVM  this._push(op1AND && op2AND);
                    break;
                // case inst.defs.XOR:
                case inst.defs.NOT:
                    this._push(Convert.ToInt32(this._pop() != 0));  //TODO: MVM (push(!this
                    break;
                // case inst.defs.INN:
                // case inst.defs.UNI:
                // case inst.defs.INT:
                // case inst.defs.DIF:
                // case inst.defs.CMP:
                // case inst.defs.SGS:
                case inst.defs.UJP:
                    this.pc = operand2;
                    break;
                case inst.defs.XJP:
                    this.pc = this._pop();
                    break;
                case inst.defs.FJP:
                    if (this._pop() == 0) //TODO: MVM - check (!this._pop())
                    {
                        this.pc = operand2;
                    }
                    break;
                case inst.defs.TJP:
                    if (this._pop() != 0)
                    {
                        this.pc = operand2;
                    }
                    break;
                case inst.defs.FLT:
                    // Cast Integer to Real.
                    // Nothing to do, we don't distinguish between integers and real.
                    break;
                // case inst.defs.FLO:
                // case inst.defs.TRC:
                // case inst.defs.RND:
                // case inst.defs.CHR:
                // case inst.defs.ORD:
                case inst.defs.STP:
                    // Stop.
                    this.stopProgram();
                    break;
                case inst.defs.LDA:
                    // Load Address. Pushes the address of a variable.
                    var addressLDA = this._computeAddress(operand1, operand2);
                    this._push(addressLDA);
                    break;
                case inst.defs.LDC:
                    // Load Constant.
                    if (operand1 == inst.defs.I || operand1 == inst.defs.R ||
                        operand1 == inst.defs.S || operand1 == inst.defs.A)
                    {

                        // Look up the constant in the constant pool.
                        this._push((int)this.bytecode.constants[operand2]);
                    }
                    else if (operand1 == inst.defs.B)
                    {
                        // Booleans are stored in operand2.
                        //TODO: MVM  this._push(!!operand2);
                    }
                    else if (operand1 == inst.defs.C)
                    {
                        // Characters are stored in operand2.
                        this._push(operand2);
                    }
                    else
                    {
                        throw new PascalError(null, "can't push constant of type " +
                                             inst.defs.typeCodeToName(operand1));
                    }
                    break;
                case inst.defs.LDI:
                    // Load Indirect.
                    var addressLDI = this._pop();
                    this._checkDataAddress(addressLDI);
                    this._push(this.dstore[addressLDI]);
                    break;
                case inst.defs.LVA:
                case inst.defs.LVB:
                case inst.defs.LVC:
                case inst.defs.LVI:
                case inst.defs.LVR:
                    // Load Value.
                    var addressLV = this._computeAddress(operand1, operand2);
                    this._checkDataAddress(addressLV);
                    this._push(this.dstore[addressLV]);
                    break;
                // case inst.defs.LVS:
                case inst.defs.STI:
                    // Store Indirect.
                    var value = this._pop();
                    var addressSTI = this._pop();
                    this._checkDataAddress(addressSTI);
                    this.dstore[addressSTI] = value;
                    break;
                case inst.defs.IXA:
                    // Indexed Address. a = a + index*stride
                    var addressIXA = this._pop();
                    var index = this._pop();
                    addressIXA += index * operand2;
                    this._push(addressIXA);
                    break;
                default:
                    throw new PascalError(null, "don't know how to execute instruction " +
                                         inst.defs.opcodeToName[opcode]);
            }
        }

        // Given a level and an offset, returns the address in the dstore. The level is
        // the number of static links to dereference.
        public int _computeAddress(int level,int offset)
        {
            var mp = this.mp;

            // Follow static link "level" times.
            for (var i = 0; i < level; i++)
            {
                mp = this._getStaticLink(mp);
            }

            return mp + offset;
        }

        // Allocate "size" words on the heap and return the new address. Throws if no
        // more heap is available.
        public int _malloc(int size)
        {
            // Make room for the object.
            this.np -= size;
            var address = this.np;

            // Blank out new allocation.
            for (var i = 0; i < size; i++)
            {
                this.dstore[address + i] = 0;
            }

            // Store size of allocation one word before the object.
            this.np--;
            this.dstore[this.np] = size;

            return address;
        }

        // Free the block on the heap pointed to by p.
        public void _free(int p)
        {
            // Get the size. We wrote it in the word before p.
            var size = this.dstore[p - 1];

            if (p == this.np + 1)
            {
                // This block is at the bottom of the heap. Just reclaim the memory.
                this.np += size + 1;
            }
            else
            {
                // Internal node. Not handled.
            }
        }

          

    }
}
