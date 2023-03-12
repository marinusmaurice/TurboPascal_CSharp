using static System.Runtime.InteropServices.JavaScript.JSType;
using System;

namespace tpc
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string source = File.ReadAllText("SPIDER.PAS");

            var DUMP_TREE = true;
            var DUMP_BYTECODE = true;
            var DEBUG_TRACE = false;

            var stream = new Stream(source);
            var lexer = new CommentStripper(new Lexer(stream));
            var parser = new Parser(lexer);

            try
            {
                // Create the symbol table of built-in constants, functions, and procedures.
                var builtinSymbolTable = SymbolTable.makeBuiltinSymbolTable();

                // Parse the program into a parse tree. Create the symbol table as we go.
                var before = DateTime.Now.Ticks;
                var root = parser.parse(builtinSymbolTable);
                /// console.log("Parsing: " + (new Date().getTime() - before) + "ms");
                if (DUMP_TREE)
                {
                    var output = root.print("");
                    //$("#parseTree").text(output);
                    Console.WriteLine(output);
                }
                /*
                // Compile to bytecode.
                before = new Date().getTime();
                var compiler = new Compiler();
                var bytecode = compiler.compile(root);
                /// console.log("Code generation: " + (new Date().getTime() - before) + "ms");
                if (DUMP_BYTECODE)
                {
                    var output = bytecode.print();
                $("#bytecode").text(output);
                }

                // Execute the bytecode.
                var machine = new Machine(bytecode, this.keyboard);
                var $state = $("#state");
                if (DEBUG_TRACE)
                {
                    machine.setDebugCallback(function(state) {
                    $state.append(state + "\n");
                    });
                }
                machine.setFinishCallback(function(runningTime) {
                /// console.log("Finished program: " + runningTime + "s");
                $("#canvas").hide();
                $("#screen").show();
                    self.printPrompt();
                });
                machine.setOutputCallback(function(line) {
                    self.screen.print(line);
                    self.screen.newLine();
                });
                machine.setInputCallback(function(callback) {
                    self.screen.addCursor();
                    self._setInputMode(INPUT_STRING, function(line) {
                        self._setInputMode(INPUT_RUNNING);
                        callback(line);
                    });
                });

                this._setInputMode(INPUT_RUNNING);
                machine.run();
               */
            }
            catch (Exception e)
            {
                // Print parsing errors.
                //if (e is PascalError) {
                //    console.log(e.getMessage());
                //    this.screen.printBold(e.getMessage());
                //    this.screen.newLine();
                //    this.printPrompt();
                //}
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}