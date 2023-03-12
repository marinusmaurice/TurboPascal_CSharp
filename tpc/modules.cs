using System.Xml.Linq;

namespace tpc
{
    internal class modules
    {
        internal static void importModule(string name, SymbolTable symbolTable)
        {
            switch (name.ToLower())
            {
                case "__builtin__":
                    builtin.importSymbols(symbolTable);
                    break;
                case "crt":
                    crt.importSymbols(symbolTable);
                    break;
                case "dos":
                    // I don't know what goes in here.
                    break;
                case "graph":
                    graph.importSymbols(symbolTable);
                    break;
                case "mouse":
                    mouse.importSymbols(symbolTable);
                    break;
                case "printer":
                    // I don't know what goes in here.
                    break;
                default:
                    throw new PascalError(null, "unknown module " + name);
            }
        }
    }
}