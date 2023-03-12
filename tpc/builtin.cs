using System.Drawing;
using System;
using System.Reflection.Metadata.Ecma335;

namespace tpc
{
    // Builtin symbols, such as "Sin()" and "Pi".

    // Special handling of Random() because its return type depends on whether
    // it has an argument.
    internal class builtin
    {



        // Import all the symbols for the builtins.
        public static void importSymbols(SymbolTable symbolTable)
        {
            // Built-in types.
            symbolTable.addNativeType("String", Node.stringType);
            symbolTable.addNativeType("Integer", Node.integerType);
            symbolTable.addNativeType("ShortInt", Node.integerType);
            symbolTable.addNativeType("LongInt", Node.integerType);
            symbolTable.addNativeType("Char", Node.charType);
            symbolTable.addNativeType("Boolean", Node.booleanType);
            symbolTable.addNativeType("Real", Node.realType);
            symbolTable.addNativeType("Double", Node.realType);
            symbolTable.addNativeType("Pointer", Node.pointerType);

            // Constants and functions.
            symbolTable.addNativeConstant("Nil", null,
                new Node(Node.SIMPLE_TYPE, new Token("Nil", Token.IDENTIFIER),
                new Dictionary<string, object> {
                            { "typeCode",  inst.defs.A },
                            { "typeName", null },  // Important -- this is what makes this nil.
                            { "type", null }
                }));
            symbolTable.addNativeConstant("True", true, Node.booleanType);
            symbolTable.addNativeConstant("False", false, Node.booleanType);
            symbolTable.addNativeConstant("Pi", Math.PI, Node.realType);


            //TODO: MVM
            symbolTable.addNativeFunction("Sin", Node.realType, new List<Node>() { Node.realType },
                       (double t) => { return Math.Sin(t); });
            symbolTable.addNativeFunction("Cos", Node.realType, new List<Node>() { Node.realType },
                       (double t) => { return Math.Cos(t); });
            symbolTable.addNativeFunction("Round", Node.integerType, new List<Node>() { Node.realType },
                       (double t) => { return Math.Round(t); });
            symbolTable.addNativeFunction("Trunc", Node.integerType, new List<Node>() { Node.realType },
                         (double t) => { return (t < 0) ? Math.Ceiling(t) : Math.Floor(t); });
            symbolTable.addNativeFunction("Odd", Node.booleanType, new List<Node>() { Node.realType },
                        (double t) => { return Math.Round(t) % 2 != 0; });
            symbolTable.addNativeFunction("Abs", Node.realType, new List<Node>() { Node.realType },
                         (double t) => { return Math.Abs(t); });
            symbolTable.addNativeFunction("Sqrt", Node.realType, new List<Node>() { Node.realType },
                         (double t) => { return Math.Sqrt(t); });
            symbolTable.addNativeFunction("Ln", Node.realType, new List<Node>() { Node.realType },
                        (double t) => { return Math.Log(t); });
            symbolTable.addNativeFunction("Sqr", Node.realType, new List<Node>() { Node.realType },
                         (double t) => { return t * t; });
            symbolTable.addNativeFunction("Random", Node.realType, new List<Node>(),
                (double t) =>
                {
                    Random rand = new Random();
                    if (t == null)
                    {
                        return rand.NextDouble();
                    }
                    else
                    {
                        return Math.Round(rand.NextDouble() * t);
                    }
                });
            symbolTable.addNativeFunction("Randomize", Node.voidType, new List<Node>(),
                        () => { /* Nothing. */ });
            var symbol = symbolTable.addNativeFunction("Inc", Node.voidType,

                new List<Node>() { Node.integerType, Node.integerType }, (Machine.Control ctl, int v, int? dv) =>
                {

                    if (dv == null)
                    {
                        dv = 1;
                    }
                    ctl.writeDstore(v, ctl.readDstore(v) + dv.Value);
                });
            symbol.type.parameters[0].byReference = true;
            symbolTable.addNativeFunction("WriteLn", Node.voidType, new List<Node>(), (Machine.Control ctl) =>
            {
                // Skip ctl parameter.
                var elements = new List<string>();
                //TODO: MVM
                //for (var i = 1; i < arguments.length; i++)
                //{
                //    // Convert to string.
                //    elements.Add("" + arguments[i]);
                //}
                ctl.writeln(string.Join(" ", elements.ToArray()));
            });
            symbolTable.addNativeFunction("ReadLn", Node.stringType, new List<Node>(), (Machine.Control ctl) =>
            {
                // Suspend the machine so that the browser can get keys to us.
                ctl.suspend();

                // Ask the IDE to read a line for us.
                ctl.readln((string line) =>
                {
                    ctl.push(Convert.ToInt32(line));
                    ctl.resume();
                });

                // We're a function, so we should return something, but we've
                // suspended the machine, so it doesn't matter.
            });
            symbolTable.addNativeFunction("Halt", Node.voidType, new List<Node>(), (Machine.Control ctl) =>
            // Halt VM.
               { ctl.stop(); });
            symbolTable.addNativeFunction("Delay", Node.voidType, new List<Node>() { Node.integerType },
                                          (Machine.Control ctl, int ms) =>
                                          {
                                              // Tell VM to delay by ms asynchronously.
                                              ctl.delay(ms);
                                          });
            symbol = symbolTable.addNativeFunction("New", Node.voidType,

                                         new List<Node>() { Node.pointerType, Node.integerType },
                                          (Machine.Control ctl, int p, int size) =>
                                          {

                                              // Allocate and store address in p.
                                              ctl.writeDstore(p, ctl.malloc(size));
                                          });
            symbol.type.parameters[0].byReference = true;
            symbol = symbolTable.addNativeFunction("GetMem", Node.voidType,

                                          new List<Node>() { Node.pointerType, Node.integerType },
                                           (Machine.Control ctl, int p, int size) =>
                                           {
                                               // Allocate and store address in p.
                                               ctl.writeDstore(p, ctl.malloc(size));
                                           });
            symbol.type.parameters[0].byReference = true;
            symbol = symbolTable.addNativeFunction("Dispose", Node.voidType,

                                         new List<Node>() { Node.pointerType },
                                            (Machine.Control ctl, int p) =>
                                            {
                                                // Free p and store 0 (nil) into it.
                                                ctl.free(ctl.readDstore(p));
                                                ctl.writeDstore(p, 0);
                                            });
            symbol.type.parameters[0].byReference = true;
        }
    }
}
