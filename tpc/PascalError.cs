using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace tpc
{
    // Exception for parse, compile, and runtime errors.
    public class PascalError : Exception
    {
        public Token token;
        public string message;

        public PascalError(Token token, string message)
        {
            this.token = token;
            this.message = message;

            // Grab a stack trace.
            //TODO: MVM this.stack = new Error().stack;
        }

        public string getMessage()
        {
            var message = "Error: " + this.message;

            // Add token info.
            if (this.token != null)
            {
                message += " (\"" + this.token.value + "\", line " + this.token.lineNumber + ")";
            }

            return message;
        }


    }
}
