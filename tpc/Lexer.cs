using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tpc
{
    internal class Lexer
    {
        // Lexer, returning tokens, including peeking.

        // define(["utils", "Token", "PascalError"], function (utils, Token, PascalError) {
        // Whether to print tokens as they're read.
        bool PRINT_TOKENS = false;
        private Stream stream;
        private Token nextToken;

        public Lexer(Stream stream)
        {
            this.stream = stream;
            this.nextToken = null;

            for (var i = 0; i < RESERVED_WORDS.Length; i++)
            {
                RESERVED_WORDS_MAP[RESERVED_WORDS[i]] = true;
            }
        }

        // All valid symbols.
        string[] SYMBOLS = new string[]{"<", "<>", "<<", ":", ":=", ">", ">>", "<=", ">=", "-", "+",
                "*", "/", ";", ",", "[", "]", "(", ")", "=", "^", "@", "(*" };

        // All reserved words.
        string[] RESERVED_WORDS = new string[]{"program", "var", "begin", "end", "type", "procedure", "function",
                "uses", "for", "while", "repeat", "do", "then", "if", "else", "to", "downto", "until",
                "array", "of", "not", "record", "or", "and", "div", "mod", "const", "exit" };
        Dictionary<string, bool> RESERVED_WORDS_MAP = new Dictionary<string, bool>();
        

        public bool isReservedWord(string value)
        {
            //TODO: MVM  return RESERVED_WORDS_MAP.hasOwnProperty(value.toLowerCase());
            return RESERVED_WORDS_MAP[value.ToLower()];
        }

        // Returns the next token.
        public Token next()
        {
            var token = this.peek();

            // We've used up this token, force the next next() or peek() to fetch another.
            this.nextToken = null;

            return token;
        }

        // Peeks at the next token.
        public Token peek()
        {
            // Fetch another token if necessary.
            if (this.nextToken == null)
            {
                this.nextToken = this._fetchNextToken();
            }

            return this.nextToken;
        }

        // Always gets another token.
        public Token _fetchNextToken()
        {
            var ch = '\0';  //TODO: MVM
            var lineNumber = -1; //TODO: MVM

            // Skip whitespace.
            do
            {
                // Keep this updated as we walk through the whitespace.
                lineNumber = this.stream.lineNumber;

                ch = this.stream.next();
                if (ch == -1)
                {
                    return new Token(null, Token.EOF);
                }
            } while (utils.isWhitespace(ch));

            // Check each type of token.
            var token = this._pickLongestToken(ch, SYMBOLS);
            if (token != null && token.isSymbol("(*"))
            {
                // Comment.

                // Keep reading until we get "*)".
                var value = "";
                while (true)
                {
                    ch = this.stream.next();
                    if (ch == -1)
                    {
                        break;
                    }
                    else if (ch == '*' && this.stream.peek() == ')')
                    {
                        // Skip ")".
                        this.stream.next();
                        break;
                    }
                    value += ch;
                }
                token = new Token(value, Token.COMMENT);
            }
            if (token == null && utils.isIdentifierStart(ch))
            {
                // Keep adding more characters until we're not part of this token anymore.
                var value = "";
                while (true)
                {
                    value += ch;
                    ch = this.stream.peek();
                    if (ch == -1 || !utils.isIdentifierPart(ch))
                    {
                        break;
                    }
                    this.stream.next();
                }
                var tokenType = isReservedWord(value) ? Token.RESERVED_WORD : Token.IDENTIFIER;
                token = new Token(value, tokenType);
            }
            if (token == null && (utils.isDigit(ch) || ch == '.'))
            {
                if (ch == '.')
                {
                    // This could be a number, a dot, or two dots.
                    var nextCh = this.stream.peek();
                    if (nextCh == '.')
                    {
                        // Two dots.
                        this.stream.next();
                        token = new Token("..", Token.SYMBOL);
                    }
                    else if (!utils.isDigit(nextCh))
                    {
                        // Single dot.
                        token = new Token(".", Token.SYMBOL);
                    }
                    else
                    {
                        // It's a number, leave token null.
                    }
                }
                if (token == null)
                {
                    // Parse number. Keep adding more characters until we're not
                    // part of this token anymore.
                    var value = "";
                    var sawDecimalPoint = ch == '.';
                    var sawExp = false;
                    var justSawExp = false;
                    while (true)
                    {
                        value += ch;
                        ch = this.stream.peek();
                        if (ch == -1)
                        {
                            break;
                        }
                        if (ch == '.' && !sawExp)
                        {
                            // This may be a decimal point, but it may be the start
                            // of a ".." symbol. Peek twice and push back.
                            this.stream.next();
                            var nextCh = this.stream.peek();
                            this.stream.pushBack(ch);
                            if (nextCh == '.')
                            {
                                // Double dot, end of number.
                                break;
                            }

                            // Now see if this single point is part of us or a separate symbol.
                            if (sawDecimalPoint)
                            {
                                break;
                            }
                            else
                            {
                                // Allow one decimal point.
                                sawDecimalPoint = true;
                            }
                        }
                        else if (ch.ToString().ToLower() == "e" && !sawExp)
                        {
                            // Start exponential section.
                            sawExp = true;
                            justSawExp = true;
                        }
                        else if (justSawExp)
                        {
                            if (ch == '+' || ch == '-' || utils.isDigit(ch))
                            {
                                // All good, this is required after "e".
                                justSawExp = false;
                            }
                            else
                            {
                                // Not allowed after "e".
                                token = new Token(value + ch, Token.NUMBER);
                                token.lineNumber = lineNumber;
                                throw new PascalError(token, "Unexpected character \"" + ch +
                                                "\" while reading exponential form");
                            }
                        }
                        else if (!utils.isDigit(ch))
                        {
                            break;
                        }
                        this.stream.next();
                    }
                    token = new Token(value, Token.NUMBER);
                }
            }
            if (token == null && ch == '{')
            {
                // Comment.

                // Skip opening brace.
                ch = this.stream.next();

                // Keep adding more characters until we're not part of this token anymore.
                var value = "";
                while (true)
                {
                    value += ch;
                    ch = this.stream.next();
                    if (ch == -1 || ch == '}')
                    {
                        break;
                    }
                }
                token = new Token(value, Token.COMMENT);
            }
            if (token == null && ch == '\'')
            {
                // String literal.

                // Skip opening quote.
                ch = this.stream.next();

                // Keep adding more characters until we're not part of this token anymore.
                var value = "";
                while (true)
                {
                    value += ch;
                    ch = this.stream.next();
                    if (ch == '\'')
                    {
                        // Handle double quotes.
                        if (this.stream.peek() == '\'')
                        {
                            // Eat next quote. First one will be added at top of loop.
                            this.stream.next();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (ch == -1)
                    {
                        break;
                    }
                }
                token = new Token(value, Token.STRING);
            }
            if (token == null)
            {
                // Unknown token.
                token = new Token(ch.ToString(), Token.SYMBOL);
                token.lineNumber = lineNumber;
                throw new PascalError(token, "unknown symbol");
            }
            token.lineNumber = lineNumber;

            if (PRINT_TOKENS)
            {
                Console.WriteLine("Fetched token \"" + token.value + "\" of type " +
                            token.tokenType + " on line " + token.lineNumber);
            }

            return token;
        }

        // Find the longest symbols in the specified list. Returns a Token or null.
       public Token _pickLongestToken(char ch,string[] symbols)
        {
            string longestSymbol = null;
            var nextCh = this.stream.peek();
            var twoCh = nextCh == -1 ? ch : ch + nextCh;

            for (var i = 0; i < symbols.Length; i++)
            {
                var symbol = symbols[i];

                if ((symbol.Length == 1 && ch.ToString() == symbol) ||
                    (symbol.Length == 2 && twoCh.ToString() == symbol))
                {

                    if (longestSymbol == null || symbol.Length > longestSymbol.Length)
                    {
                        longestSymbol = symbol;
                    }
                }
            }

            if (longestSymbol == null)
            {
                return null;
            }

            if (longestSymbol.Length == 2)
            {
                // Eat the second character.
                this.stream.next();
            }

            return new Token(longestSymbol, Token.SYMBOL);
        }
         

    }
}