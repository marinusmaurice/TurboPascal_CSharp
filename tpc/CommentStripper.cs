using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 

namespace tpc
{
    // A token filter that strips out comment tokens.
    internal class CommentStripper
    {
        private Lexer lexer;

        public CommentStripper(Lexer lexer) {
                this.lexer = lexer;
            }

            // Returns the next token.
            public Token next() {
                while (true)
                {
                    var token = this.lexer.next();
                    if (token.tokenType != Token.COMMENT)
                    {
                        return token;
                    }
                }
            }

            // Peeks at the next token.
            public Token peek() {
                while (true)
                {
                    var token = this.lexer.peek();
                    if (token.tokenType != Token.COMMENT)
                    {
                        return token;
                    }
                    else
                    {
                        // Skip the comment.
                        this.lexer.next();
                    }
                }
            }
        }
}
