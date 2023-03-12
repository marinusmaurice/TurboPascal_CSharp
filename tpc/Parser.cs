using System.Xml.Linq;

namespace tpc
{
    // Parses a lexer's output into a tree of Node objects.
    internal class Parser
    {
        private CommentStripper lexer;

        public Parser(CommentStripper lexer)
        {
            this.lexer = lexer;
        }

        // Parse an entire Pascal program.
        public Node parse(SymbolTable symbolTable)
        {
            var node = this._parseSubprogramDeclaration(symbolTable, Node.PROGRAM);

            return node;
        }


        // Returns whether there are more entities to come. The function is given
        // two symbols, one that's a separator and one's that a terminator. Returns
        // true and eats the symbol if it sees the separator; returns false and
        // leaves the symbol if it sees the terminator. Throws if it sees anything else.
        public bool _moreToCome(string separator, string terminator)
        {
            var token = this.lexer.peek();
            if (token.isSymbol(separator))
            {
                // More to come. Eat the separator.
                this.lexer.next();
                return true;
            }
            else if (token.isSymbol(terminator))
            {
                // We're done. Leave the terminator.
                return false;
            }
            else
            {
                throw new PascalError(token, "expected \"" + separator +
                                      "\" or \"" + terminator + "\"");
            }
        }

        // Eats the next symbol. If it's not this reserved word, raises an error with this
        // message. Returns the token.
        public Token _expectReservedWord(string reservedWord, string message = null)
        {
            var token = this.lexer.next();
            if (message == null)
                message = ("expected reserved word \"" + reservedWord + "\"");
            if (!token.isReservedWord(reservedWord))
            {
                throw new PascalError(token, message);
            }
            return token;
        }

        // Eats the next symbol (such as ":="). If it's not this symbol, raises an
        // error with this message. Returns the token.
        public Token _expectSymbol(string symbol, string message = null)
        {
            var token = this.lexer.next();
            if (token.tokenType != Token.SYMBOL || token.value != symbol)
            {
                if (message == null)
                    message = ("expected symbol \"" + symbol + "\"");
                throw new PascalError(token, message);
            }
            return token;
        }

        // Eats the next symbol. If it's not an identifier, raises an error with this
        // message. Returns the identifier token.
        public Token _expectIdentifier(string message)
        {
            var token = this.lexer.next();
            if (token.tokenType != Token.IDENTIFIER)
            {
                throw new PascalError(token, message);
            }
            return token;
        }

        // Returns a list of declarations (var, etc.).
        public Dictionary<string, Node> _parseDeclarations(SymbolTable symbolTable)
        {
            var declarations = new Dictionary<string, Node>();

            // Parse each declaration or block.
            while (!this.lexer.peek().isReservedWord("begin"))
            {
                // This parser also eats the semicolon after the declaration.
                var nodes = this._parseDeclaration(symbolTable);

                // Extend the declarations array with the nodes array.
               //TODO: MVM declarations.push.apply(declarations, nodes);
            }

            return declarations;
        }

        // Parse any declaration (uses, var, procedure, function). Returns a list
        // of them, in case a declaration expands to be multiple nodes.
        public List<Node> _parseDeclaration(SymbolTable symbolTable)
        {
            var token = this.lexer.peek();

            if (token.isReservedWord("uses"))
            {
                return this._parseUsesDeclaration(symbolTable);
            }
            else if (token.isReservedWord("var"))
            {
                this._expectReservedWord("var");
                return this._parseVarDeclaration(symbolTable);
            }
            else if (token.isReservedWord("const"))
            {
                this._expectReservedWord("const");
                return this._parseConstDeclaration(symbolTable);
            }
            else if (token.isReservedWord("type"))
            {
                this._expectReservedWord("type");
                return this._parseTypeDeclaration(symbolTable);
            }
            else if (token.isReservedWord("procedure"))
            {
                return new List<Node>() { this._parseSubprogramDeclaration(symbolTable, Node.PROCEDURE) };
            }
            else if (token.isReservedWord("function"))
            {
                return new List<Node>() { this._parseSubprogramDeclaration(symbolTable, Node.FUNCTION) };
            }
            else if (token.tokenType == Token.EOF)
            {
                throw new PascalError(token, "unexpected end of file");
            }
            else
            {
                throw new PascalError(token, "unexpected token");
            }
        }

        // Parse "uses" declaration, which is a list of identifiers. Returns a list of nodes.
        public List<Node> _parseUsesDeclaration(SymbolTable symbolTable)
        {
            var usesToken = this._expectReservedWord("uses");

            var nodes = new List<Node>();

            do
            {
                var token = this._expectIdentifier("expected module name");
                var node = new Node(Node.USES, usesToken, new Dictionary<string, object>{
                    { "name", new Node(Node.IDENTIFIER, token)}
                    });

                // Import the module's symbols into this symbol table.
                modules.importModule(token.value, symbolTable);

                nodes.Add(node);
            } while (this._moreToCome(",", ";"));

            this._expectSymbol(";");

            return nodes;
        }

        // Parse "var" declaration, which is a variable and its type. Returns a list of nodes.
        public List<Node> _parseVarDeclaration(SymbolTable symbolTable)
        {
            var nodes = new List<Node>();

            do
            {
                var startNode = nodes.Count;

                do
                {
                    var nameToken = this._expectIdentifier("expected variable name");
                    var node = new Node(Node.VAR, null, new Dictionary<string, object>{
                        { "name", new Node(Node.IDENTIFIER, nameToken) }
                        });
                    nodes.Add(node);
                } while (this._moreToCome(",", ":"));

                // Skip colon.
                this._expectSymbol(":");

                // Parse the variable's type.
                var type = this._parseType(symbolTable);

                // Set the type of all nodes for this line.
                for (var i = startNode; i < nodes.Count; i++)
                {
                    nodes[i].type = type;

                    // Add the variable to our own symbol table.
                    nodes[i].symbol = symbolTable.addSymbol(
                        nodes[i].name.token.value, Node.VAR, type, false);
                }

                // We always finish the line with a semicolon.
                this._expectSymbol(";");

                // If the next token is an identifier, then we keep going.
            } while (this.lexer.peek().tokenType == Token.IDENTIFIER);

            return nodes;
        }

        // Parse "const" declaration, which is an identifier, optional type, and
        // required value. Returns an array of nodes.
        public List<Node> _parseConstDeclaration(SymbolTable symbolTable)
        {
            var nodes = new List<Node>();

            do
            {
                // Parse the constant name.
                var token = this._expectIdentifier("expected constant name");
                var identifierNode = new Node(Node.IDENTIFIER, token);

                // Parse optional type.
                Node type = null;
                token = this.lexer.peek();
                if (token.isSymbol(":"))
                {
                    this.lexer.next();
                    type = this._parseType(symbolTable);
                }

                // Parse value. How we do this depends on whether it's a typed constant,
                // and if it is, what kind.
                this._expectSymbol("=");

                // Create the node.
                Node node;
                if (type == null)
                {
                    // Constant.
                    var expression = this._parseExpression(symbolTable);
                    node = new Node(Node.CONST, null, new Dictionary<string, object>{
                        { "name", identifierNode },
                    { "type", expression.expressionType },
                    { "value", expression }
                });
                }
                else
                {
                    // Typed constant.
                    RawData rawData;

                    // XXX We need to verify type compatibility throughout here.
                    if (type.nodeType == Node.ARRAY_TYPE)
                    {
                        rawData = this._parseArrayConstant(symbolTable, type);
                    }
                    else if (type.nodeType == Node.RECORD_TYPE)
                    {
                        throw new PascalError(token, "constant records not supported");
                    }
                    else if (type.nodeType == Node.SIMPLE_TYPE)
                    {
                        rawData = new RawData();
                        rawData.addNode(this._parseExpression(symbolTable));
                    }
                    else
                    {
                        throw new PascalError(token, "unhandled typed constant type " + type.nodeType);
                    }

                    node = new Node(Node.TYPED_CONST, null, new Dictionary<string, object>{
                        { "name", identifierNode },
                        { "type", type },
                        { "rawData", rawData }
                });
                }

                // Add the constant to our own symbol table.
                node.symbol = symbolTable.addSymbol(identifierNode.token.value,
                                                    node.nodeType, node.type, false); //TODO: MVM false
                if (type == null)
                {
                    node.symbol.value = node.value;
                }
                nodes.Add(node);

                // Semicolon terminator.
                this._expectSymbol(";");
            } while (this.lexer.peek().tokenType == Token.IDENTIFIER);

            return nodes;
        }

        // Parse an array constant, which is a parenthesized list of constants. These
        // can be nested for multi-dimensional arrays. Returns a RawData object.
        public RawData _parseArrayConstant(SymbolTable symbolTable, Node type)
        {
            // The raw linear (in-memory) version of the data.
            var rawData = new RawData();

            // Recursive function to parse a dimension of the array. The first
            // dimension (ranges[0]) is the "major" one, and we recurse until
            // the last dimension, where we actually parse the constant
            // expressions.
            var self = this;
            void parseDimension(int d)
            {
                self._expectSymbol("(");

                var low = type.ranges[d].getRangeLowBound();
                var high = type.ranges[d].getRangeHighBound();
                for (var i = low; i <= high; i++)
                {
                    if (d == type.ranges.Length - 1)
                    {
                        // Parse the next constant.
                        rawData.addNode(self._parseExpression(symbolTable));
                    }
                    else
                    {
                        parseDimension(d + 1);
                    }
                    if (i < high)
                    {
                        self._expectSymbol(",");
                    }
                }

                self._expectSymbol(")");
            }

            // Start the recursion.
            parseDimension(0);

            return rawData;
        }

        // Parse "type" declaration, which is an identifier and a type. Returns an
        // array of nodes.
        public List<Node> _parseTypeDeclaration(SymbolTable symbolTable)
        {
            var nodes = new List<Node>();

            // Pointer types are permitted to point to an undefined type name, as long as
            // that name is defined by the end of the "type" section. We keep track of these
            // here and resolve them at the end.
            var incompleteTypes = new List<Node>();

            do
            {
                // Parse identifier.
                var token = this._expectIdentifier("expected type name");
                var identifierNode = new Node(Node.IDENTIFIER, token);

                // Required equal sign.
                var equalToken = this._expectSymbol("=");

                // Parse type.
                var type = this._parseType(symbolTable, incompleteTypes);

                // Create the node.
                var node = new Node(Node.TYPE, equalToken, new Dictionary<string, object>{
                    { "name", identifierNode },
                    { "type", type }
            });

                // Add the type to our own symbol table.
                node.symbol = symbolTable.addType(identifierNode.token.value, type);
                nodes.Add(node);

                // Semicolon terminator.
                this._expectSymbol(";");
            } while (this.lexer.peek().tokenType == Token.IDENTIFIER);

            // Fill in incomplete types. They're required to be defined by the end of
            // the "type" block.
            for (int i = 0; i < incompleteTypes.Count; i++)
            {
                var node = incompleteTypes[i];

                node.type = symbolTable.getType(node.typeName.token).symbol.type;
            }

            return nodes;
        }

        // Parse procedure, function, or program declaration.
        public Node _parseSubprogramDeclaration(SymbolTable symbolTable, int nodeType)
        {
            // Get the string like "procedure", etc.
            var declType = Node.nodeLabel[nodeType];

            // Parse the opening token.
            var procedureToken = this._expectReservedWord(declType);

            // Parse the name.
            var nameToken = this._expectIdentifier("expected " + declType + " name");

            // From now on we're in our own table.
            var symbolTableLocal = new SymbolTable(symbolTable);

            // Parse the parameters.
            var token = this.lexer.peek();
            var parameters = new List<Node>();
            if (token.isSymbol("("))
            {
                this._expectSymbol("(");

                var start = 0;
                do
                {
                    var byReference = false;

                    // See if we're passing this batch by reference.
                    if (this.lexer.peek().isReservedWord("var"))
                    {
                        this._expectReservedWord("var");
                        byReference = true;
                    }

                    Token colon1 = null;
                    // Parameters can be batched by type.
                    do
                    {
                        token = this._expectIdentifier("expected parameter name");
                        parameters.Add(new Node(Node.PARAMETER, colon1, new Dictionary<string, object>{
                            { "name", new Node(Node.IDENTIFIER, token) },
                            { "byReference", byReference }
                    }));
                    } while (this._moreToCome(",", ":"));
                    var colon = this._expectSymbol(":");

                    // Add the type to each parameter.
                    var type1 = this._parseType(symbolTableLocal);
                    for (var i = start; i < parameters.Count; i++)
                    {
                        parameters[i].type = type1;
                    }
                    start = parameters.Count;
                } while (this._moreToCome(";", ")"));

                this._expectSymbol(")");
            }

            // Add parameters to our own symbol table.
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                var symbol1 = symbolTableLocal.addSymbol(parameter.name.token.value, Node.PARAMETER,
                                                   parameter.type, parameter.byReference);
            }

            // Parse the return type if it's a function.
            Node returnType;
            if (nodeType == Node.FUNCTION)
            {
                this._expectSymbol(":");
                returnType = this._parseType(symbolTableLocal);
            }
            else
            {
                returnType = Node.voidType;
            }
            this._expectSymbol(";");

            // Functions have an additional fake symbol: their own name, which maps
            // to the mark pointer location (return value).
            if (nodeType == Node.FUNCTION)
            {
                var name = nameToken.value;
                symbolTableLocal.symbols[name.ToLower()] = new Symbol(name, returnType, 0, false);
            }

            // Create the type of the subprogram itself.
            var type = new Node(Node.SUBPROGRAM_TYPE, procedureToken, new Dictionary<string, object>{

                { "parameters", parameters },
                { "returnType", returnType },
        });

            // Add the procedure to our parent symbol table.
            var symbol = symbolTable.parentSymbolTable.addSymbol(nameToken.value,
                                                                 Node.SUBPROGRAM_TYPE, type, false); //TODO: MVM false

            // Parse declarations.
            var declarations = this._parseDeclarations(symbolTable);

            // Parse begin/end block.
            var block = this._parseBlock(symbolTable, "begin", "end");

            // Make node.
            var node = new Node(nodeType, procedureToken, new Dictionary<string, object> {




                { "name", new Node(Node.IDENTIFIER, nameToken) },
                { "declarations", declarations },
                { "block", block }
        });
            node.symbol = symbol;
            node.symbolTable = symbolTable;
            node.expressionType = type;

            // Semicolon terminator.
            this._expectSymbol(nodeType == Node.PROGRAM ? "." : ";");

            return node;
        }

        // Parse a begin/end block. The startWord must be the next token. The endWord
        // will end the block and is eaten.
        public Node _parseBlock(SymbolTable symbolTable, string startWord, string endWord)
        {
            var token = this._expectReservedWord(startWord);
            var statements = new List<Node>();

            var foundEnd = false;
            while (!foundEnd)
            {
                token = this.lexer.peek();
                if (token.isReservedWord(endWord))
                {
                    // End of block.
                    this.lexer.next();
                    foundEnd = true;
                }
                else if (token.isSymbol(";"))
                {
                    // Empty statement.
                    this.lexer.next();
                }
                else
                {
                    // Parse statement.
                    statements.Add(this._parseStatement(symbolTable));

                    // After an actual statement, we require a semicolon or end of block.
                    token = this.lexer.peek();
                    if (!token.isReservedWord(endWord) && !token.isSymbol(";"))
                    {
                        throw new PascalError(token, "expected \";\" or \"" + endWord + "\"");
                    }
                }
            }

            return new Node(Node.BLOCK, token, new Dictionary<string, object>{
                { "statements", statements }
        });
        }

        // Parse a statement, such as a for loop, while loop, assignment, or procedure call.
        public Node _parseStatement(SymbolTable symbolTable)
        {
            var token = this.lexer.peek();
            Node node;

            // Handle simple constructs.
            if (token.isReservedWord("if"))
            {
                node = this._parseIfStatement(symbolTable);
            }
            else if (token.isReservedWord("while"))
            {
                node = this._parseWhileStatement(symbolTable);
            }
            else if (token.isReservedWord("repeat"))
            {
                node = this._parseRepeatStatement(symbolTable);
            }
            else if (token.isReservedWord("for"))
            {
                node = this._parseForStatement(symbolTable);
            }
            else if (token.isReservedWord("begin"))
            {
                node = this._parseBlock(symbolTable, "begin", "end");
            }
            else if (token.isReservedWord("exit"))
            {
                node = this._parseExitStatement(symbolTable);
            }
            else if (token.tokenType == Token.IDENTIFIER)
            {
                // This could be an assignment or procedure call. Both start with an identifier.
                node = this._parseVariable(symbolTable);

                // See if this is an assignment or procedure call.
                token = this.lexer.peek();
                if (token.isSymbol(":="))
                {
                    // It's an assignment.
                    node = this._parseAssignment(symbolTable, node);
                }
                else if (node.nodeType == Node.IDENTIFIER)
                {
                    // Must be a procedure call.
                    node = this._parseProcedureCall(symbolTable, node);
                }
                else
                {
                    throw new PascalError(token, "invalid statement");
                }
            }
            else
            {
                throw new PascalError(token, "invalid statement");
            }

            return node;
        }

        // Parse a variable. A variable isn't just an identifier, like "foo", it can also
        // be an array dereference, like "variable[index]", a field designator, like
        // "variable.fieldName", or a pointer dereference, like "variable^". In all
        // three cases the "variable" part is itself a variable. This function always
        // returns a node of type IDENTIFIER, ARRAY, FIELD_DESIGNATOR, or DEREFERENCE.
        public Node _parseVariable(SymbolTable symbolTable)
        {
            // Variables always start with an identifier.
            var identifierToken = this._expectIdentifier("expected identifier");

            // Create an identifier node for this token.
            var node = new Node(Node.IDENTIFIER, identifierToken);

            // Look up the symbol so we can set its type.
            var symbolLookup = symbolTable.getSymbol(identifierToken);
            node.symbolLookup = symbolLookup;
            node.expressionType = symbolLookup.symbol.type;

            // The next token determines whether the variable continues or ends here.
            while (true)
            {
                var nextToken = this.lexer.peek();
                if (nextToken.isSymbol("["))
                {
                    // Replace the node with an array node.
                    node = this._parseArrayDereference(symbolTable, node);
                }
                else if (nextToken.isSymbol("."))
                {
                    // Replace the node with a record designator node.
                    node = this._parseRecordDesignator(symbolTable, node);
                }
                else if (nextToken.isSymbol("^"))
                {
                    // Replace the node with a pointer dereference.
                    this._expectSymbol("^");
                    var variable = node;
                    if (!variable.expressionType.isSimpleType(inst.defs.A))
                    {
                        throw new PascalError(nextToken, "can only dereference pointers");
                    }
                    node = new Node(Node.DEREFERENCE, nextToken, new Dictionary<string, object>{
                        { "variable", node }
                });
                    node.expressionType = variable.expressionType.type;
                }
                else
                {
                    // We're done with the variable.
                    break;
                }
            }

            return node;
        }

        // Parse an assignment. We already have the left-hand-side variable.
        public Node _parseAssignment(SymbolTable symbolTable, Node variable)
        {
            var assignToken = this._expectSymbol(":=");

            var expression = this._parseExpression(symbolTable);
            return new Node(Node.ASSIGNMENT, assignToken, new Dictionary<string, object>{
                { "lhs", variable },
                { "rhs", expression.castToType(variable.expressionType) }
        });
        }

        // Parse a procedure call. We already have the identifier, so we only need to
        // parse the optional arguments.
        public Node _parseProcedureCall(SymbolTable symbolTable, Node identifier)
        {
            // Look up the symbol to make sure it's a procedure.
            var symbolLookup = symbolTable.getSymbol(identifier.token);
            var symbol = symbolLookup.symbol;
            identifier.symbolLookup = symbolLookup;

            // Verify that it's a procedure.
            if (symbol.type.nodeType == Node.SUBPROGRAM_TYPE && symbol.type.returnType.isVoidType())
            {
                // Parse optional arguments.
                var argumentList = this._parseArguments(symbolTable, symbol.type);

                // If the call is to the native function "New", then we pass a hidden second
                // parameter, the size of the object to allocate. The procedure needs that
                // to know how much to allocate.
                if (symbol.name.ToLower() == "new" && symbol.isNative)
                {
                    if (argumentList.Count == 1)
                    {
                        argumentList.Add(Node.makeNumberNode(
                            argumentList[0].expressionType.type.getTypeSize().ToString()));
                    }
                    else
                    {
                        throw new PascalError(identifier.token, "new() takes one argument");
                    }
                }

                return new Node(Node.PROCEDURE_CALL, identifier.token, new Dictionary<string, object>{
                    { "name", identifier },
                    { "argumentList", argumentList }
            });
            }
            else
            {
                throw new PascalError(identifier.token, "expected procedure");
            }
        }

        // Parse an optional argument list. Returns a list of nodes. type is the
        // type of the subprogram being called.
        public List<Node> _parseArguments(SymbolTable symbolTable, Node type)
        {
            var argumentList = new List<Node>();

            if (this.lexer.peek().isSymbol("("))
            {
                this._expectSymbol("(");
                var token = this.lexer.peek();
                if (token.isSymbol(")"))
                {
                    // Empty arguments.
                    this.lexer.next();
                }
                else
                {
                    do
                    {
                        // Find the formal parameter. Some functions (like WriteLn)
                        // are variadic, so allow them to have more arguments than
                        // were defined.
                        var argumentIndex = argumentList.Count;
                        Node parameter;
                        if (argumentIndex < type.parameters.Length)
                        {
                            parameter = type.parameters[argumentIndex];
                        }
                        else
                        {
                            // Accept anything (by value).
                            parameter = null;
                        }

                        Node argument;
                        if (parameter != null && parameter.byReference)
                        {
                            // This has to be a variable, not any expression, since
                            // we need its address.
                            argument = this._parseVariable(symbolTable);

                            // Hack this "byReference" field that'll be used by
                            // the compiler to pass the argument's address.
                            argument.byReference = true;
                        }
                        else
                        {
                            argument = this._parseExpression(symbolTable);
                        }

                        // Cast to type of parameter.
                        if (parameter != null)
                        {
                            argument = argument.castToType(parameter.type);
                        }

                        argumentList.Add(argument);
                    } while (this._moreToCome(",", ")"));
                    this._expectSymbol(")");
                }
            }

            return argumentList;
        }

        // Parse an if statement.
        public Node _parseIfStatement(SymbolTable symbolTable)
        {
            var token = this._expectReservedWord("if");

            var expression = this._parseExpression(symbolTable);
            if (!expression.expressionType.isBooleanType())
            {
                throw new PascalError(expression.token, "if condition must be a boolean");
            }

            this._expectReservedWord("then");
            var thenStatement = this._parseStatement(symbolTable);

            Node elseStatement = null;
            var elseToken = this.lexer.peek();
            if (elseToken.isReservedWord("else"))
            {
                this._expectReservedWord("else");
                //var elseStatement = this._parseStatement(symbolTable);  //TODO: MVM
                elseStatement = this._parseStatement(symbolTable);
            }

            return new Node(Node.IF, token, new Dictionary<string, object>{
                { "expression", expression },
                { "thenStatement", thenStatement },
                { "elseStatement", elseStatement }
        });
        }

        // Parse a while statement.
        public Node _parseWhileStatement(SymbolTable symbolTable)
        {
            var whileToken = this._expectReservedWord("while");

            // Parse the expression that keeps the loop going.
            var expression = this._parseExpression(symbolTable);
            if (!expression.expressionType.isBooleanType())
            {
                throw new PascalError(whileToken, "while condition must be a boolean");
            }

            // The "do" keyword is required.
            this._expectReservedWord("do", "expected \"do\" for \"while\" loop");

            // Parse the statement. This can be a begin/end pair.
            var statement = this._parseStatement(symbolTable);

            // Create the node.
            return new Node(Node.WHILE, whileToken, new Dictionary<string, object>{
                { "expression", expression },
                { "statement", statement }
        });
        }

        // Parse a repeat/until statement.
        public Node _parseRepeatStatement(SymbolTable symbolTable)
        {
            var block = this._parseBlock(symbolTable, "repeat", "until");
            var expression = this._parseExpression(symbolTable);
            
            if (!expression.expressionType.isBooleanType())
            {
                //TODO: MVM throw new PascalError(node.token, "repeat condition must be a boolean");
                throw new PascalError(null, "repeat condition must be a boolean");
            }

            return new Node(Node.REPEAT, block.token, new Dictionary<string, object>{
                { "block", block },
                { "expression", expression }
        });
        }

        // Parse a for statement.
        public Node _parseForStatement(SymbolTable symbolTable)
        {
            var token = this._expectReservedWord("for");

            var loopVariableToken = this._expectIdentifier("expected identifier for \"for\" loop");
            this._expectSymbol(":=");
            var fromExpr = this._parseExpression(symbolTable);
            var downto = this.lexer.peek().isReservedWord("downto");
            if (downto)
            {
                this._expectReservedWord("downto");
            }
            else
            {
                // Default error message if it's neither.
                this._expectReservedWord("to");
            }
            var toExpr = this._parseExpression(symbolTable);
            this._expectReservedWord("do");
            var body = this._parseStatement(symbolTable);

            // Get the symbol for the loop variable.
            var symbolLookup = symbolTable.getSymbol(loopVariableToken);
            var loopVariableType = symbolLookup.symbol.type;
            var variable = new Node(Node.IDENTIFIER, loopVariableToken);
            variable.symbolLookup = symbolLookup;

            // Cast "from" and "to" to type of variable.
            fromExpr = fromExpr.castToType(loopVariableType);
            toExpr = toExpr.castToType(loopVariableType);

            return new Node(Node.FOR, token, new Dictionary<string, object>{
            { "variable", variable },
            { "fromExpr", fromExpr },
            { "toExpr", toExpr },
            { "body", body },
            { "downto", downto }
        });
        }

        // Parse an exit statement.
        public Node _parseExitStatement(SymbolTable symbolTable)
        {
            var token = this._expectReservedWord("exit");

            return new Node(Node.EXIT, token);
        }

        // Parse a type declaration, such as "Integer" or "Array[1..70] of Real".
        // The "incompleteTypes" array is optional. If specified, and if a pointer
        // to an unknown type is found, it is added to the array. If such a pointer
        // is found and the array was not passed in, we throw.
        public Node _parseType(SymbolTable symbolTable, List<Node> incompleteTypes= null)
        {
            var token = this.lexer.next();
            Node node;

            if (token.isReservedWord("array"))
            {
                // Array type.
                this._expectSymbol("[");
                var ranges = new List<Node>();
                // Parse multiple ranges.
                do
                {
                    var range = this._parseRange(symbolTable);
                    ranges.Add(range);
                } while (this._moreToCome(",", "]"));
                this._expectSymbol("]");
                this._expectReservedWord("of");
                var elementType = this._parseType(symbolTable, incompleteTypes);

                node = new Node(Node.ARRAY_TYPE, token, new Dictionary<string, object> {
                    { "elementType", elementType },
                    { "ranges", ranges}
            });
            }
            else if (token.isReservedWord("record"))
            {
                node = this._parseRecordType(symbolTable, token, incompleteTypes);
            }
            else if (token.isSymbol("^"))
            {
                var typeNameToken = this._expectIdentifier("expected type identifier");
                Node type;
                try
                {
                    type = symbolTable.getType(typeNameToken).symbol.type;
                }
                catch (Exception e)
                {
                    if (e is PascalError)
                    {
                        // The type symbol is not defined. Pascal requires that it be defined
                        // by the time the "type" section ends.
                        type = null;
                    }
                    else
                    {
                        throw new PascalError(typeNameToken, "exception looking up type symbol");
                    }
                }
                node = new Node(Node.SIMPLE_TYPE, token, new Dictionary<string, object>{
                    { "typeCode", inst.defs.A },
                    { "typeName", new Node(Node.IDENTIFIER, typeNameToken) },
                    { "type", type }
            });
                // See if this is a forward type reference.
                if (type == null)
                {
                    // We'll fill these in later.
                    if (incompleteTypes != null)
                    {
                        incompleteTypes.Add(node);
                    }
                    else
                    {
                        throw new PascalError(typeNameToken, "unknown type");
                    }
                }
            }
            else if (token.tokenType == Token.IDENTIFIER)
            {
                // Type name.
                var symbolLookup = symbolTable.getType(token);

                // Substitute the type right away. This will mess up the display of
                // the program, since you'll see the full type everywhere, but will
                // simplify the compilation step.
                node = symbolLookup.symbol.type;
            }
            else
            {
                throw new PascalError(token, "can't parse type");
            }

            // A type node is its own type.
            node.expressionType = node;

            return node;
        }

        // Parse a record type definition. See _parseType() for an explanation of "incompleteTypes".
        public Node _parseRecordType(SymbolTable symbolTable, Token token, List<Node> incompleteTypes)
        {
            // A record is a list of fields.
            var fields = new List<Node>();

            while (true)
            {
                var token2 = this.lexer.peek();
                if (token.isSymbol(";"))
                {
                    // Empty field, no problem.
                    this.lexer.next();
                }
                else if (token2.isReservedWord("end"))
                {
                    // End of record.
                    this._expectReservedWord("end");
                    break;
                }
                else
                {
                    //fields.push.apply(fields,
                    fields.AddRange(
                                      this._parseRecordSection(symbolTable, token2, incompleteTypes));
                    // Must have ";" or "end" after field.
                    var token1 = this.lexer.peek();
                    if (!token.isSymbol(";") && !token1.isReservedWord("end"))
                    {
                        throw new PascalError(token1, "expected \";\" or \"end\" after field");
                    }
                }
            }

            // Calculate the offset of each field.
            var offset = 0;
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                field.offset = offset;
                offset += (int)field.type.getTypeSize();
            }

            return new Node(Node.RECORD_TYPE, token, new Dictionary<string, object>{
                { "fields", fields }
        });
        }

        // Parse a section of a record type, which is a list of identifiers and
        // their type. Returns an array of FIELD nodes. See _parseType() for an
        // explanation of "incompleteTypes".
        public List<Node> _parseRecordSection(SymbolTable symbolTable, Token fieldToken, List<Node> incompleteTypes)
        {
            var fields = new List<Node>();

            do
            {
                var nameToken = this._expectIdentifier("expected field name");
                var field = new Node(Node.FIELD, fieldToken, new Dictionary<string, object>{
                    { "name", new Node(Node.IDENTIFIER, nameToken) },
                    { "offset", 0 }
                    });
                fields.Add(field);
            } while (this._moreToCome(",", ":"));

            // Skip colon.
            this._expectSymbol(":");

            // Parse the fields's type.
            var type = this._parseType(symbolTable, incompleteTypes);

            // Set the type of all fields.
            for (var i = 0; i < fields.Count; i++)
            {
                fields[i].type = type;
            }

            return fields;
        }

        // Parses a range, such as "5..10". Either can be a constant expression.
        public Node _parseRange(SymbolTable symbolTable)
        {
            var low = this._parseExpression(symbolTable);
            var token = this._expectSymbol("..");
            var high = this._parseExpression(symbolTable);

            return new Node(Node.RANGE, token, new Dictionary<string, object> { { "low", low }, { "high", high } });
        }

        // Parses an expression.
        public Node _parseExpression(SymbolTable symbolTable)
        {
            return this._parseRelationalExpression(symbolTable);
        }

        // Parses a relational expression.
        public Node _parseRelationalExpression(SymbolTable symbolTable)
        {
            var node = this._parseAdditiveExpression(symbolTable);

            while (true)
            {
                var token = this.lexer.peek();
                if (token.isSymbol("="))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.EQUALITY,
                            this._parseAdditiveExpression).withExpressionType(Node.booleanType);
                }
                else if (token.isSymbol("<>"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.INEQUALITY,
                            this._parseAdditiveExpression).withExpressionType(Node.booleanType);
                }
                else if (token.isSymbol(">"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.GREATER_THAN,
                            this._parseAdditiveExpression).withExpressionType(Node.booleanType);
                }
                else if (token.isSymbol("<"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.LESS_THAN,
                            this._parseAdditiveExpression).withExpressionType(Node.booleanType);
                }
                else if (token.isSymbol(">="))
                {
                    node = this._createBinaryNode(symbolTable, token, node,
                                                  Node.GREATER_THAN_OR_EQUAL_TO,
                            this._parseAdditiveExpression).withExpressionType(Node.booleanType);
                }
                else if (token.isSymbol("<="))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.LESS_THAN_OR_EQUAL_TO,
                            this._parseAdditiveExpression).withExpressionType(Node.booleanType);
                }
                else
                {
                    break;
                }
            }

            return node;
        }

        // Parses an additive expression.
        public Node _parseAdditiveExpression(SymbolTable symbolTable)
        {
            var node = this._parseMultiplicativeExpression(symbolTable);

            while (true)
            {
                var token = this.lexer.peek();
                if (token.isSymbol("+"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.ADDITION,
                                                  this._parseMultiplicativeExpression);
                }
                else if (token.isSymbol("-"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.SUBTRACTION,
                                                  this._parseMultiplicativeExpression);
                }
                else if (token.isReservedWord("or"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.OR,
                                                  this._parseMultiplicativeExpression,
                                                  Node.booleanType);
                }
                else
                {
                    break;
                }
            }

            return node;
        }

        // Parses a multiplicative expression.
        public Node _parseMultiplicativeExpression(SymbolTable symbolTable)
        {
            var node = this._parseUnaryExpression(symbolTable);

            while (true)
            {
                var token = this.lexer.peek();
                if (token.isSymbol("*"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.MULTIPLICATION,
                                                  this._parseUnaryExpression, null);
                }
                else if (token.isSymbol("/"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.DIVISION,
                                                  this._parseUnaryExpression, Node.realType);
                }
                else if (token.isReservedWord("div"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.INTEGER_DIVISION,
                                                  this._parseUnaryExpression, Node.integerType);
                }
                else if (token.isReservedWord("mod"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.MOD,
                                                  this._parseUnaryExpression, Node.integerType);
                }
                else if (token.isReservedWord("and"))
                {
                    node = this._createBinaryNode(symbolTable, token, node, Node.AND,
                                                  this._parseUnaryExpression, Node.booleanType);
                }
                else
                {
                    break;
                }
            }

            return node;
        }

        // Parses a unary expression, such as a negative sign or a "not".
        public Node _parseUnaryExpression(SymbolTable symbolTable)
        {
            Node node;

            // Parse unary operator.
            var token = this.lexer.peek();
            if (token.isSymbol("-"))
            {
                // Negation.
                this._expectSymbol("-");

                var expression = this._parseUnaryExpression(symbolTable);
                node = new Node(Node.NEGATIVE, token, new Dictionary<string, object>{
                    { "expression", expression }
            }).withExpressionTypeFrom(expression);
            }
            else if (token.isSymbol("+"))
            {
                // Unary plus.
                this._expectSymbol("+");

                // Nothing to wrap sub-expression with.
                node = this._parseUnaryExpression(symbolTable);
            }
            else if (token.isReservedWord("not"))
            {
                // Logical not.
                this._expectReservedWord("not");

                var expression = this._parseUnaryExpression(symbolTable);
                if (!expression.expressionType.isBooleanType())
                {
                    throw new PascalError(expression.token, "not operand must be a boolean");
                }
                node = new Node(Node.NOT, token, new Dictionary<string, object>{
                    { "expression",expression }
            }).withExpressionTypeFrom(expression);
            }
            else
            {
                node = this._parsePrimaryExpression(symbolTable);
            }

            return node;
        }

        // Parses an atomic expression, such as a number, identifier, or
        // parenthesized expression.
        public Node _parsePrimaryExpression(SymbolTable symbolTable)
        {
            var token = this.lexer.peek();
            Node node;

            if (token.tokenType == Token.NUMBER)
            {
                // Numeric literal.
                token = this.lexer.next();
                node = new Node(Node.NUMBER, token);
                var v = node.getNumber();
                int typeCode;

                // See if we're an integer or real.
                if (Math.Abs(v)- (int)(Math.Abs(v)) > 0)
                {
                    typeCode = inst.defs.R;
                }
                else
                {
                    typeCode = inst.defs.I;
                } 
                // Set the type based on the kind of number we have. Really we should
                // have the lexer tell us, because JavaScript treats "2.0" the same as "2".
                node.expressionType = new Node(Node.SIMPLE_TYPE, token, new Dictionary<string, object>{
                    { "typeCode", typeCode }
            });
            }
            else if (token.tokenType == Token.STRING)
            {
                // String or character literal.
                token = this.lexer.next();
                node = new Node(Node.STRING, token);
                var v = node.token.value;

                node.expressionType = new Node(Node.SIMPLE_TYPE, token, new Dictionary<string, object> {
                    // String literal of length 1 is a Char.
                    { "typeCode", v.Length == 1 ? inst.defs.C : inst.defs.S }
            });
            }
            else if (token.tokenType == Token.IDENTIFIER)
            {
                // Parse a variable (identifier, array dereference, etc.).
                node = this._parseVariable(symbolTable);

                // What we do next depends on the variable. If it's just an identifier,
                // then it could be a function call, a function call with arguments,
                // a constant, or a plain variable. We handle all these cases. If it's
                // not just an identifier, then we leave it alone.
                if (node.nodeType == Node.IDENTIFIER)
                {
                    // Peek to see if we've got parentheses.
                    var nextToken = this.lexer.peek();

                    // Look up the symbol.
                    SymbolLookup symbolLookup;
                    if (nextToken.isSymbol("("))
                    {
                        // This is a hack to allow recursion. I don't know how a real Pascal
                        // parser might distinguish between a function and an identifier. Do
                        // we first check the parenthesis or first check the symbol type?
                        symbolLookup = symbolTable.getSymbol(node.token, Node.SUBPROGRAM_TYPE);
                    }
                    else
                    {
                        symbolLookup = symbolTable.getSymbol(node.token);
                    }
                    var symbol = symbolLookup.symbol;
                    node.symbolLookup = symbolLookup;

                    if (symbol.type.nodeType == Node.SUBPROGRAM_TYPE)
                    {
                        // We're calling a function. Make sure it's not a procedure.
                        if (symbol.type.returnType.isVoidType())
                        {
                            throw new PascalError(node.token, "can't call procedure in expression");
                        }

                        // Make the function call node with the optional arguments.
                        node = new Node(Node.FUNCTION_CALL, node.token, new Dictionary<string, object>{
                            { "name", node },
                            { "argumentList", this._parseArguments(symbolTable, symbol.type) }
                    });

                        // Type of the function call is the return type of the function.
                        node.expressionType = symbol.type.returnType;

                        // We have to hack the call to Random() because its return
                        // type depends on whether it takes a parameter or not.
                        // We detect that we're calling the built-in one and modify
                        // the return type to be an Integer if it takes a parameter.
                        if (symbol.name.ToLower() == "random" &&
                            symbol.isNative &&
                            node.argumentList.Length > 0)
                        {

                            // Return Integer.
                            node.expressionType = Node.integerType;
                        }

                        // Hack Abs() because its return type is the same as its parameter.
                        // If the parameter was an integer, then it's already been cast
                        // to a real in the argument parsing.
                        if (symbol.name.ToLower() == "abs" &&
                            symbol.isNative &&
                            node.argumentList.Length == 1 &&
                            node.argumentList[0].nodeType == Node.CAST)
                        {

                            node.expressionType = node.argumentList[0].expression.expressionType;
                        }
                    }
                    else
                    {
                        // This is just a symbol. Check to see if it's a constant. If it is,
                        // replace it with the value.
                        if (symbol.value != null)
                        {
                            // Only for simple types.
                            node = symbol.value;
                        }
                        else
                        {
                            // Normal variable. Look up its type.
                            node.expressionType = symbol.type;
                        }
                    }
                }
            }
            else if (token.isSymbol("("))
            {
                // Parenthesized expression.
                this._expectSymbol("(");
                node = this._parseExpression(symbolTable);
                this._expectSymbol(")");
            }
            else if (token.isSymbol("@"))
            {
                // This doesn't work. It's not clear what the type of the resulting
                // expression is. It should be a pointer to a (say) integer, but
                // a pointer type requires a typeName, which we don't have and might
                // not have at all. If this variable is declared as being of type
                // record, then there's no name to use. And even if it uses a formal
                // type definition, we lose than when we look up the type of the variable.
                // None of our code uses this expression, so we're not going to support
                // it.
                throw new PascalError(token, "the @ operator is not supported");

                //TODO: MVM
                ////////    this._expectSymbol("@");
                ////////    var variable = this._parseVariable(symbolTable);
                ////////    node = new Node(Node.ADDRESS_OF, token, {
                ////////    variable: variable
                ////////});
                ////////    node.expressionType = new Node(Node.SIMPLE_TYPE, token, {
                ////////    typeCode: inst.A,
                ////////    typeName: "AD-HOC",
                ////////    type: variable.expressionType
                ////////});
            }
            else
            {
                throw new PascalError(token, "expected expression");
            }

            return node;
        }

        // Parse an array dereference, such as "a[2,3+4]".
        public Node _parseArrayDereference(SymbolTable symbolTable, Node variable)
        {
            // Make sure the variable is an array.
            if (variable.expressionType.nodeType != Node.ARRAY_TYPE)
            {
                throw new PascalError(variable.token, "expected an array type");
            }

            var arrayToken = this._expectSymbol("[");
            var indices = new List<Node>();
            do
            {
                // Indices must be integers.
                indices.Add(this._parseExpression(symbolTable).castToType(Node.integerType));
            } while (this._moreToCome(",", "]"));
            this._expectSymbol("]");

            var array = new Node(Node.ARRAY, arrayToken, new Dictionary<string, object>{
                { "variable", variable },
                { "indices", indices }
                    });

            // The type of the array lookup is the type of the array element.
            array.expressionType = variable.expressionType.elementType;

            return array;
        }

        // Parse a record designator, such as "a.b".
        public Node _parseRecordDesignator(SymbolTable symbolTable, Node variable)
        {
            // Make sure the variable so far is a record.
            var recordType = variable.expressionType;
            Token nextToken = null;
            if (recordType.nodeType != Node.RECORD_TYPE)
            {
                throw new PascalError(nextToken, "expected a record type");
            }

            var dotToken = this._expectSymbol(".", "expected a dot");

            // Parse the field name.
            var fieldToken = this._expectIdentifier("expected a field name");

            // Get the field for this identifier.
            var field = recordType.getField(fieldToken);

            // Create the new node.
            var node = new Node(Node.FIELD_DESIGNATOR, dotToken, new Dictionary<string, object> {
                { "variable", variable},
                { "field", field}
            });

            // Type of designation is the type of the field.
            node.expressionType = field.type;

            return node;
        }

        // Creates a binary node.
        //
        // token: the specific token, which must be next in the lexer.
        // node: the first (left) operand.
        // nodeType: the type of the binary node (Node.ADDITION, etc.).
        // rhsFn: the function to call to parse the RHS. It should take a symbolTable object
        //      and return an expression node.
        // forceType: optional type node (e.g., Node.realType). Both operands will be cast
        //      naturally to this type and the node will be of this type.
        public Node _createBinaryNode(SymbolTable symbolTable, Token token, Node node,
                                                     int nodeType, Func<SymbolTable, Node> rhsFn, Node forceType = null)
        {

            // It must be next, we've only peeked at it.
            if (token.tokenType == Token.SYMBOL)
            {
                this._expectSymbol(token.value);
            }
            else
            {
                this._expectReservedWord(token.value);
            }

            var operand1 = node;
            var operand2 = rhsFn(symbolTable);

            Node expressionType;
            if (forceType != null)
            {
                // Use what's passed in.
                expressionType = forceType;
            }
            else
            {
                // Figure it out from the operands.
                expressionType = this._getCompatibleType(token,
                                                         operand1.expressionType,
                                                         operand2.expressionType);
            }

            // Cast the operands if necessary.
            node = new Node(nodeType, token, new Dictionary<string, object>{
                { "lhs", operand1.castToType(expressionType) },
                { "rhs", operand2.castToType(expressionType) }
        }).withExpressionType(expressionType);

            return node;
        }

        // Returns a type compatible for both operands. For example, if one is
        // integer and another is real, returns a real, since you can implicitly
        // cast from integer to real. Throws if a compatible type can't
        // be found. Token is passed in just for error reporting.
        public Node _getCompatibleType(Token token, Node type1, Node type2)
        {
            // Must have them defined.
            if (type1 == null)
            {
                throw new PascalError(token, "can't find compatible types for type1=null");
            }
            if (type2 == null)
            {
                throw new PascalError(token, "can't find compatible types for type2=null");
            }

            // Must be the same type of node. Can't cast between node types
            // (e.g., array to set).
            if (type1.nodeType != type2.nodeType)
            {
                throw new PascalError(token, "basic types are incompatible: " +
                                     type1.print() + " and " + type2.print());
            }

            // Can cast between some simple types.
            if (type1.nodeType == Node.SIMPLE_TYPE &&
                type1.typeCode != type2.typeCode)
            {

                // They're different.
                var typeCode1 = type1.typeCode;
                var typeCode2 = type2.typeCode;

                if (typeCode1 == inst.defs.A || typeCode2 == inst.defs.A ||
                    typeCode1 == inst.defs.B || typeCode2 == inst.defs.B ||
                    typeCode1 == inst.defs.S || typeCode2 == inst.defs.S ||
                    typeCode1 == inst.defs.T || typeCode2 == inst.defs.T ||
                    typeCode1 == inst.defs.P || typeCode2 == inst.defs.P ||
                    typeCode1 == inst.defs.X || typeCode2 == inst.defs.X)
                {

                    // These can't be cast.
                    throw new PascalError(token, "no common type between " +
                                         inst.defs.typeCodeToName(typeCode1) +
                                         " and " + inst.defs.typeCodeToName(typeCode2));
                }

                // Can always cast to a real.
                if (typeCode1 == inst.defs.R)
                {
                    return type1;
                }
                else if (typeCode2 == inst.defs.R)
                {
                    return type2;
                }

                // Otherwise can cast to an integer.
                if (typeCode1 == inst.defs.I)
                {
                    return type1;
                }
                else if (typeCode2 == inst.defs.I)
                {
                    return type2;
                }

                // I don't know how we got here.
                throw new PascalError(token, "internal compiler error, can't determine " +
                                     "common type of " + typeCode1 + " and " + typeCode2);
            }
            else
            {
                // Return either type.
                return type1;
            }
        }



    }
}