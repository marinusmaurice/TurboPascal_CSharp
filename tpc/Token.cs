using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tpc
{
    public class Token
    {
        public Token(string value, int tokenType)
        {
            this.value = value;
            this.tokenType = tokenType;
            this.lineNumber = -1;
        }

        // Token types.
        public static int IDENTIFIER = 0;
        public static int NUMBER = 1;
        public static int SYMBOL = 2;
        public static int COMMENT = 3;
        public static int STRING = 4;
        public static int EOF = 5;
        public static int RESERVED_WORD = 6;
        public string value;
        public int tokenType;
        public int lineNumber;

        // Returns whether this token is a reserved word, such as "for". These are
        // case-insensitive.
        public bool isReservedWord(string reservedWord)
        {
            return this.tokenType == Token.RESERVED_WORD &&
                this.value.ToLower() == reservedWord.ToLower();
        }

        // Returns whether this token is equal to the specified token. The line
        // number is not taken into account; only the type and value.
        public bool isEqualTo(Token other)
        {
            return this.tokenType == other.tokenType && this.value == other.value;
        }

        // Returns whether this is the specified symbol.
        public bool isSymbol(string symbol)
        {
            return this.tokenType == Token.SYMBOL && this.value == symbol;
        }

    }
}
