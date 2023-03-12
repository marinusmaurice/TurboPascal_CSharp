using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace tpc
{
    // Model class for syntactic nodes.
    internal class Node
    {
        //define(["inst", "PascalError", "Token", "utils"], function (inst, PascalError, Token, utils) {

        public int nodeType;
        internal Token token;
        internal SymbolTable symbolTable;
        internal Node expressionType;
        internal SymbolLookup symbolLookup;
        public Symbol symbol;
        internal int typeCode;
        internal Node low;
        internal Node high;
        internal Node[] fields;
        internal Node[] parameters;
        internal Node elementType;
        internal Node[] ranges;
        internal Node rhs;
        internal Node name;
        internal Node[] statements;
        internal Node[] declarations;
        internal Node block;
        internal Node type;
        internal Node expression;
        internal Node lhs;
        internal Node variable;
        internal Node[] indices;
        internal Node typeName;
        internal bool byReference;
        internal Node value;
        internal static Dictionary<int, string> nodeLabel = new Dictionary<int, string>();
        internal Node thenStatement;
        internal Node elseStatement;
        internal Node statement;
        internal Node fromExpr;
        internal Node toExpr;
        internal Node body;
        internal RawData rawData;
        internal Node returnType;
        internal bool downto;
        internal dynamic field;
        internal Node[] argumentList;
        internal int offset;

        public Node(int nodeType, Token token, dynamic additionalFields = null)//Node[] additionalFields = null)
        {
            // The type of node (e.g., Node.PROGRAM), see below.
            this.nodeType = nodeType;

            // The token that created this node.
            this.token = token;

            // Symbol table (for node types PROGRAM, PROCEDURE, and FUNCTION).
            this.symbolTable = null;

            // Type of this node (for expressions).
            this.expressionType = null;

            // Symbol in the symbol table (if VAR, CONST, etc.).
            this.symbol = null;

            // Symbol lookup in the symbol table (if IDENTIFIER, ARRAY, FUNCTION_CALL, etc.).
            this.symbolLookup = null;

            // Fold other fields into our own.
            if (additionalFields != null)
            {
                foreach (KeyValuePair<string, object> field in additionalFields)
                {
                    //TODO: MVM this[field] = additionalFields[field];
                    Type typeInQuestion = typeof(Node);
                    FieldInfo f = typeInQuestion.GetField(field.Key, BindingFlags.NonPublic | BindingFlags.Instance);
                    f.SetValue(this, field.Value);
                }
            }
            // Fill in this label map.
            nodeLabel[PROGRAM] = "program";
            nodeLabel[PROCEDURE] = "procedure";
            nodeLabel[FUNCTION] = "function";
        }

        // Basic types. These don't have additional fields, but their token usually has a value.
        public const int IDENTIFIER = 0;
        public const int NUMBER = 1;
        public const int STRING = 2;
        public const int BOOLEAN = 3;
        public const int POINTER = 4;

        // Program, procedure, or function declaration.
        //     name: name of program, procedure, or function (identifier).
        //     declarations: functions, procedures, var, const, uses, etc.
        //     block: block.
        public const int PROGRAM = 10;
        public const int PROCEDURE = 11;
        public const int FUNCTION = 12;

        // Uses declaration.
        //     name: module name (identifier).
        public const int USES = 13;

        // Var declaration.
        //     name: variable name (identifier).
        //     type: variable type.
        public const int VAR = 14;

        // Range of ordinals.
        //     low: lowest index (number).
        //     high: highest index (number).
        public const int RANGE = 15;

        // Begin/end block.
        //     statements: statements.
        public const int BLOCK = 16;

        // Function and procedure parameter.
        //     name: parameter name (identifier).
        //     type: type.
        //     byReference: whether this parameter is by reference.
        public const int PARAMETER = 17;

        // Cast expression to type.
        //     type: destination type.
        //     expression: source node.
        public const int CAST = 18;

        // Constant declaration.
        //     name: variable name (identifier).
        //     type: type.
        //     value: value.
        public const int CONST = 19;

        // Assignment.
        //     lhs: variable being assigned to.
        //     rhs: expression to assign.
        public const int ASSIGNMENT = 20;

        // Procedure call statement.
        //     name: procedure name.
        //     argumentList: procedure arguments.
        public const int PROCEDURE_CALL = 21;

        // Repeat/until.
        //     block: block.
        //     expression: expression.
        public const int REPEAT = 22;

        // For loop.
        //     variable: variable (identifier).
        //     fromExpr: from expression.
        //     toExpr: to expression.
        //     body: body statement.
        //     downto: whether it's a downto loop (true) or to (false).
        public const int FOR = 23;

        // If.
        //     expression: expression.
        //     thenStatement: then statement.
        //     elseStatement: else statement or null.
        public const int IF = 24;

        // Exit.
        //     No additional fields.
        public const int EXIT = 25;

        // Record field.
        //     name: field name (identifier).
        //     type: type.
        //     offset: integer offset from base of record.
        public const int FIELD = 26;

        // While loop.
        //     expression: expression.
        //     statement: statement to loop.
        public const int WHILE = 27;

        // Typed constant. These are really pre-initialized variables.
        //     name: constant name (identifier).
        //     type: declared type.
        //     rawData: a RawData object.
        public const int TYPED_CONST = 28;

        // Unary operators.
        //     expression: expression to act on.
        public const int NOT = 30;
        public const int NEGATIVE = 31;

        // Binary operators. Children are lhs and rhs.
        public const int ADDITION = 40;
        public const int SUBTRACTION = 41;
        public const int MULTIPLICATION = 42;
        public const int DIVISION = 43;
        public const int EQUALITY = 44;
        public const int INEQUALITY = 45;
        public const int LESS_THAN = 46;
        public const int GREATER_THAN = 47;
        public const int LESS_THAN_OR_EQUAL_TO = 48;
        public const int GREATER_THAN_OR_EQUAL_TO = 49;
        public const int AND = 50;
        public const int OR = 51;
        public const int INTEGER_DIVISION = 52;
        public const int MOD = 53;

        // Field designator (expression.fieldName).
        //     variable: the part before the dot, which evaluates to a record type.
        //     field: designated field (FIELD).
        public const int FIELD_DESIGNATOR = 54;

        // Function call expression.
        //     name: function name (identifier).
        //     argumentList: arguments (expressions).
        public const int FUNCTION_CALL = 60;

        // Array dereference.
        //     variable: expression that evaluates to an array.
        //     indices: expression for each index.
        public const int ARRAY = 61;

        // Type definition.
        //     name: name of new type (identifier).
        //     type: aliased type.
        public const int TYPE = 62;

        // Address-of (@) operator.
        //     variable: variable to take the address of.
        public const int ADDRESS_OF = 63;

        // Dereference of a pointer (^).
        //     variable: variable to dereference.
        public const int DEREFERENCE = 64;

        // Simple type.
        //     typeCode: one of inst.defs.A, inst.defs.B, inst.defs.C, inst.defs.I, inst.defs.R, or inst.defs.S.
        //     typeName: (inst.defs.A only) name of the type being pointed to. This must be a name
        //         and not a type because we can point to ourselves or have
        //         mutually-referring types.
        //     type: (inst.defs.A only) type being pointed to. This can initially be null, but is
        //         filled in once we have enough types to resolve the type name.
        public const int SIMPLE_TYPE = 70;

        // Enumerated type.
        //     entries: each entry (identifier).
        public const int ENUM_TYPE = 71;

        // Record type.
        //     fields: FIELD nodes.
        public const int RECORD_TYPE = 73;

        // Array type.
        //     elementType: element type.
        //     ranges: RANGE nodes.
        public const int ARRAY_TYPE = 74;

        // Set type.
        //     type: type of element (integral SIMPLE_TYPE or ENUM_TYPE).
        //     range: optional RANGE node.
        public const int SET_TYPE = 75;

        // Procedure, function, or program type.
        //     parameters: parameters (public const int PARAMETER).
        //     returnType: return type (SIMPLE_TYPE inst.defs.P if not function).
        public const int SUBPROGRAM_TYPE = 76;

        // Set the symbol table for this program, procedure, or function.
        public void setSymbolTable(SymbolTable symbolTable)
        {
            this.symbolTable = symbolTable;
        }

        // Logs the node in JSON format to the console.
        public void log()
        {
            Console.WriteLine("//TODO: MVM JSON.stringify(this, null, 4)");
        }

        // Returns whether the type is numeric (integer, character, or real).
        public bool isNumericType()
        {
            return this != null &&
                this.nodeType == Node.SIMPLE_TYPE &&
                (this.typeCode == inst.defs.C ||
                 this.typeCode == inst.defs.I ||
                 this.typeCode == inst.defs.R);
        }

        // Returns whether the type is boolean.
        public bool isBooleanType()
        {
            return this != null &&
                this.nodeType == Node.SIMPLE_TYPE &&
                this.typeCode == inst.defs.B;
        }

        // Returns whether the type is void (procedure return type).
        public bool isVoidType()
        {
            return this != null &&
                this.nodeType == Node.SIMPLE_TYPE &&
                this.typeCode == inst.defs.P;
        }

        // If both are identifiers, and are the same identifier (case-insensitive), returns true.
        // If identifiers and not equal, returns false. If either is not an identifier, throws.
        public bool isSameIdentifier(Node other)
        {
            if (this.nodeType != Node.IDENTIFIER || other.nodeType != Node.IDENTIFIER)
            {
                throw new PascalError(this.token, "not an identifier");
            }
            return this.token.value.ToLower() == other.token.value.ToLower();
        }

        // Given a type, returns true if it's a simple type and of the specified type code.
        public bool isSimpleType(int typeCode)
        {
            return this.nodeType == Node.SIMPLE_TYPE && this.typeCode == typeCode;
        }

        // Given a NUMBER node, returns the value as a float.
        public decimal getNumber()
        {
            if (this.nodeType == Node.NUMBER)
            {
                decimal f = 0;
                if (Decimal.TryParse(this.token.value, out f))
                {
                    return f;
                }
                else   //TODO: MVM
                {
                    throw new PascalError(this.token, "expected a number");
                }
            }
            else
            {
                throw new PascalError(this.token, "expected a number");
            }
        }

        // Given a BOOLEAN node, returns the value as a boolean.
        public bool getBoolean()
        {
            if (this.nodeType == Node.BOOLEAN)
            {
                return this.token.value.ToLower() == "true";
            }
            else
            {
                throw new PascalError(this.token, "expected a boolean");
            }
        }

        // Given a SIMPLE_TYPE node, returns the type code.
        public int getSimpleTypeCode()
        {
            if (this.nodeType == Node.SIMPLE_TYPE)
            {
                return this.typeCode;
            }
            else
            {
                throw new PascalError(this.token, "expected a simple type");
            }
        }

        // Given a RANGE node, returns the lower bound as a number.
        public decimal getRangeLowBound()
        {
            if (this.nodeType == Node.RANGE)
            {
                return this.low.getNumber();
            }
            else
            {
                throw new PascalError(this.token, "expected a range");
            }
        }

        // Given a RANGE node, returns the high bound as a number.
        public decimal getRangeHighBound()
        {
            if (this.nodeType == Node.RANGE)
            {
                return this.high.getNumber();
            }
            else
            {
                throw new PascalError(this.token, "expected a range");
            }
        }

        // Given a RANGE node, returns the size (high minus low plus 1).
        public decimal getRangeSize()
        {
            if (this.nodeType == Node.RANGE)
            {
                return this.high.getNumber() - this.low.getNumber() + 1;
            }
            else
            {
                throw new PascalError(this.token, "expected a range");
            }
        }

        // Given a RECORD_TYPE node, returns the FIELD node for the given token.
        public Node getField(Token fieldToken)  //TODO: MVM  return type
        {
            if (this.nodeType != Node.RECORD_TYPE)
            {
                throw new PascalError(this.token, "expected a record");
            }

            if (fieldToken.tokenType != Token.IDENTIFIER)
            {
                throw new PascalError(fieldToken, "expected a field name");
            }

            // We could use a dictionary for this instead of a linear lookup, but
            // it's not worth the complexity.
            for (int i = 0; i < this.fields.Length; i++)
            {
                var field = this.fields[i];
                if (field.name.token.isEqualTo(fieldToken))
                {
                    return field;
                }
            }

            throw new PascalError(fieldToken, "field not found in record");
        }

        // Given any expression type, returns the value of the expression. The
        // expression must evaluate to a scalar constant.
        public object getConstantValue()
        {
            switch (this.nodeType)
            {
                case Node.NUMBER:
                    return this.getNumber();
                case Node.BOOLEAN:
                    return this.getBoolean();
                case Node.STRING:
                    return this.token.value;
                default:
                    throw new PascalError(this.token, "cannot get constant value of node type " +
                                          this.nodeType);
            }
        }

        // Return the total parameter size of a function's parameters.
        public int getTotalParameterSize()
        {
            if (this.nodeType != Node.SUBPROGRAM_TYPE)
            {
                throw new PascalError(this.token, "can't get parameter size of non-subprogram");
            }

            var size = 0;

            for (var i = 0; i < this.parameters.Length; i++)
            {
                var parameter = this.parameters[i];
                size += parameter.byReference ? 1 : (int)parameter.type.getTypeSize();
            }

            return size;
        }

        // Given a type node (SIMPLE_TYPE, ARRAY_TYPE, etc.), returns the size of that type.
        public decimal getTypeSize()
        {
            decimal size;

            switch (this.nodeType)
            {
                case Node.SIMPLE_TYPE:
                    // They all have the same size.
                    size = 1;
                    break;
                /// case Node.ENUM_TYPE:
                case Node.RECORD_TYPE:
                    size = 0;
                    for (var i = 0; i < this.fields.Length; i++)
                    {
                        size += this.fields[i].type.getTypeSize();
                    }
                    break;
                case Node.ARRAY_TYPE:
                    // Start with size of element type.
                    size = this.elementType.getTypeSize();

                    // Multiply each range size.
                    for (var i = 0; i < this.ranges.Length; i++)
                    {
                        size *= this.ranges[i].getRangeSize();
                    }
                    break;
                /// case Node.SET_TYPE:
                default:
                    throw new PascalError(this.token, "can't get size of type " + this.print());
            }

            return size;
        }

        // Useful types.
        public static Node pointerType = new Node(SIMPLE_TYPE, null, new Dictionary<string, object> { { "typeCode", inst.defs.A } });
        public static Node booleanType = new Node(SIMPLE_TYPE, null, new Dictionary<string, object> { { "typeCode", inst.defs.B } });
        public static Node charType = new Node(SIMPLE_TYPE, null, new Dictionary<string, object> { { "typeCode", inst.defs.C } });
        public static Node integerType = new Node(SIMPLE_TYPE, null, new Dictionary<string, object> { { "typeCode", inst.defs.I } });
        public static Node voidType = new Node(SIMPLE_TYPE, null, new Dictionary<string, object> { { "typeCode", inst.defs.P } });
        public static Node realType = new Node(SIMPLE_TYPE, null, new Dictionary<string, object> { { "typeCode", inst.defs.R } });
        public static Node stringType = new Node(SIMPLE_TYPE, null, new Dictionary<string, object> { { "typeCode", inst.defs.S } });

        // Fluid method to set the expression type.
        public Node withExpressionType(Node expressionType)
        {
            this.expressionType = expressionType;
            return this;
        }
        public Node withExpressionTypeFrom(Node node)
        {
            this.expressionType = node.expressionType;
            return this;
        }

        // Useful methods.
        public static Node makeIdentifierNode(string name)
        {
            return new Node(Node.IDENTIFIER, new Token(name, Token.IDENTIFIER));
        }
        public static Node makeNumberNode(string value)
        {
            return new Node(Node.NUMBER, new Token("" + value, Token.NUMBER));
        }
        public static Node makeBooleanNode(string value)
        {
            return new Node(Node.BOOLEAN, new Token(value == null ? "True" : "False", Token.IDENTIFIER));  //TODO: MVM   value == null
        }
        public static Node makePointerNode(object value)
        {
            // Nil is the only constant pointer.
            if (value != null)
            {
                throw new PascalError(null, "nil is the only pointer constant");
            }
            return new Node(Node.POINTER, new Token("Nil", Token.IDENTIFIER));
        }

        // Maps a node type (e.g., Node.PROGRAM) to a string ("program", "procedure", or "function").
        //   Node.nodeLabel = { } // Filled below.

        // Returns printed version of node.
        public string print(string indent = null)
        {
            var s = "";

            // Allow caller to not set indent.
            //indent = indent || ""; //TODO: MVM
            if (indent == null)
                indent = "";

            switch (this.nodeType)
            {
                case Node.IDENTIFIER:
                case Node.NUMBER:
                case Node.BOOLEAN:
                case Node.POINTER:
                    s += this.token.value;
                    break;
                case Node.STRING:
                    s += "'" + this.token.value + "'";
                    break;
                case Node.PROGRAM:
                case Node.PROCEDURE:
                case Node.FUNCTION:
                    // Nest procedures and functions.
                    if (this.nodeType != Node.PROGRAM)
                    {
                        indent += "    ";
                        s += "\n";
                    }

                    s += indent + nodeLabel[this.nodeType] + " " + this.name.token.value;

                    // Print parameters and return type.
                    s += this.expressionType.print() + ";\n\n";

                    // Declarations.
                    for (var i = 0; i < this.declarations.Length; i++)
                    {
                        s += this.declarations[i].print(indent) + ";\n";
                    }

                    // Main block.
                    s += "\n" + this.block.print(indent);

                    if (this.nodeType == Node.PROGRAM)
                    {
                        s += ".\n";
                    }
                    break;
                case Node.USES:
                    s += indent + "uses " + this.name.token.value;
                    break;
                case Node.VAR:
                    s += indent + "var " + this.name.print() + " : " + this.type.print();
                    break;
                case Node.RANGE:
                    s += this.low.print() + ".." + this.high.print();
                    break;
                case Node.BLOCK:
                    s += indent + "begin\n";
                    for (var i = 0; i < this.statements.Length; i++)
                    {
                        s += this.statements[i].print(indent + "    ") + ";\n";
                    }
                    s += indent + "end";
                    break;
                case Node.PARAMETER:
                    s += (this.byReference ? "var " : "") + this.name.print() +
                        " : " + this.type.print();
                    break;
                case Node.CAST:
                    s += this.type.print() + "(" + this.expression.print() + ")";
                    break;
                case Node.CONST:
                    s += indent + "const " + this.name.print();
                    if (this.type != null)
                    {
                        s += " { : " + this.type.print() + " }";
                    }
                    s += " = " + this.value.print();
                    break;
                case Node.ASSIGNMENT:
                    s += indent + this.lhs.print() + " := " + this.rhs.print();
                    break;
                case Node.PROCEDURE_CALL:
                case Node.FUNCTION_CALL:
                    if (this.nodeType == Node.PROCEDURE_CALL)
                    {
                        s += indent;
                    }
                    s += this.name.print();
                    List<string> argumentListLocal = new List<string>();
                    for (var i = 0; i < this.argumentList.Length; i++)
                    {
                        argumentListLocal.Add(this.argumentList[i].print(indent));
                    }
                    if (argumentList.Length > 0)
                    {
                        s += "(" + string.Join(", ", argumentListLocal.ToArray()) + ")";
                    }
                    break;
                case Node.REPEAT:
                    s += indent + "repeat\n";
                    s += this.block.print(indent + "    ");
                    s += "\n" + indent + "until " + this.expression.print();
                    break;
                case Node.FOR:
                    s += indent + "for " + this.variable.print() + " := " +
                        this.fromExpr.print() + (this.downto ? " downto " : " to ") +
                        this.toExpr.print() +
                        " do\n";
                    s += this.body.print(indent + "    ");
                    break;
                case Node.IF:
                    s += indent + "if " + this.expression.print() + " then\n";
                    s += this.thenStatement.print(indent + "    ");
                    if (this.elseStatement != null)
                    {
                        s += "\n" + indent + "else\n";
                        s += this.elseStatement.print(indent + "    ");
                    }
                    break;
                case Node.EXIT:
                    s += indent + "Exit";
                    break;
                case Node.FIELD:
                    s += indent + this.name.print() + " : " + this.type.print(indent);
                    break;
                case Node.WHILE:
                    s += indent + "while " + this.expression.print() + " do\n" +
                        this.statement.print(indent + "    ");
                    break;
                case Node.TYPED_CONST:
                    s += indent + "const " + this.name.print();
                    s += " : " + this.type.print();
                    s += " = " + this.rawData.print();
                    break;
                case Node.NOT:
                    s += "Not " + this.expression.print();
                    break;
                case Node.NEGATIVE:
                    s += "-" + this.expression.print();
                    break;
                case Node.ADDITION:
                    s += this.lhs.print() + " + " + this.rhs.print();
                    break;
                case Node.SUBTRACTION:
                    s += this.lhs.print() + " - " + this.rhs.print();
                    break;
                case Node.MULTIPLICATION:
                    s += "(" + this.lhs.print() + "*" + this.rhs.print() + ")";
                    break;
                case Node.DIVISION:
                    s += this.lhs.print() + "/" + this.rhs.print();
                    break;
                case Node.EQUALITY:
                    s += this.lhs.print() + " = " + this.rhs.print();
                    break;
                case Node.INEQUALITY:
                    s += this.lhs.print() + " <> " + this.rhs.print();
                    break;
                case Node.LESS_THAN:
                    s += this.lhs.print() + " < " + this.rhs.print();
                    break;
                case Node.GREATER_THAN:
                    s += this.lhs.print() + " > " + this.rhs.print();
                    break;
                case Node.LESS_THAN_OR_EQUAL_TO:
                    s += this.lhs.print() + " <= " + this.rhs.print();
                    break;
                case Node.GREATER_THAN_OR_EQUAL_TO:
                    s += this.lhs.print() + " >= " + this.rhs.print();
                    break;
                case Node.AND:
                    s += this.lhs.print() + " and " + this.rhs.print();
                    break;
                case Node.OR:
                    s += this.lhs.print() + " or " + this.rhs.print();
                    break;
                case Node.INTEGER_DIVISION:
                    s += this.lhs.print() + " div " + this.rhs.print();
                    break;
                case Node.MOD:
                    s += this.lhs.print() + " mod " + this.rhs.print();
                    break;
                case Node.FIELD_DESIGNATOR:
                    s += this.variable.print() + "." + this.field.name.print();
                    break;
                case Node.ARRAY:
                    List<string> indicesLocal = new List<string>();
                    for (var i = 0; i < this.indices.Length; i++)
                    {
                        indicesLocal.Add(this.indices[i].print());
                    }
                    s += this.variable.print() + "[" + string.Join(",", indicesLocal.ToArray()) + "]";
                    break;
                case Node.TYPE:
                    s += indent + "type " + this.name.print() + " = " + this.type.print();
                    break;
                case Node.ADDRESS_OF:
                    s += "@" + this.variable.print();
                    break;
                case Node.DEREFERENCE:
                    s += this.variable.print() + "^";
                    break;
                case Node.SIMPLE_TYPE:
                    if (this.typeCode == inst.defs.A)
                    {
                        if (this.typeName != null)
                        {
                            s += "^" + this.typeName.print();
                        }
                        else
                        {
                            // Generic pointer.
                            s += "Pointer";
                        }
                    }
                    else
                    {
                        s += inst.defs.typeCodeToName(this.typeCode);
                    }
                    break;
                case Node.RECORD_TYPE:
                    s += "record\n";
                    for (var i = 0; i < this.fields.Length; i++)
                    {
                        s += this.fields[i].print(indent + "    ") + ";\n";
                    }
                    s += indent + "end";
                    break;
                case Node.ARRAY_TYPE:
                    List<string> rangesLocal = new List<string>();
                    for (var i = 0; i < this.ranges.Length; i++)
                    {
                        rangesLocal.Add(this.ranges[i].print());
                    }
                    s += "array[" + string.Join(",", rangesLocal.ToArray()) + "] of " + this.elementType.print();
                    break;
                case Node.SUBPROGRAM_TYPE:
                    // Print parameters.
                    List<string> parametersLocal = new List<string>();
                    for (var i = 0; i < this.parameters.Length; i++)
                    {
                        parametersLocal.Add(this.parameters[i].print());
                    }
                    if (parametersLocal.Count > 0)
                    {
                        s += "(" + string.Join("; ", parametersLocal.ToArray()) + ")";
                    }

                    // Functions only: return type.
                    if (!this.returnType.isSimpleType(inst.defs.P))
                    {
                        s += " : " + this.returnType.print();
                    }
                    break;
                default:
                    s = "<UNKNOWN>";
                    break;
            }

            return s;
        }

        // Return a node that casts "this" to "type". Returns "this" if it's already
        // of type "type". Throws if "this" can't be cast to "type".
        public Node castToType(Node type)
        {
            // If the destination type is void and we're by reference, then do nothing
            // and allow anything. We're essentially passing into an untyped "var foo"
            // parameter.
            if (type.isVoidType() && this.byReference)
            {
                return this;
            }

            // Existing type.
            var nodeType = this.expressionType;

            // Must have type defined.
            if (type == null)
            {
                throw new PascalError(this.token, "can't cast to null type");
            }
            if (nodeType == null)
            {
                throw new PascalError(this.token, "can't cast from null type");
            }

            // Must be the same type of node. Can't cast between node types
            // (e.g., array to set).
            if (type.nodeType != nodeType.nodeType)
            {
                throw new PascalError(this.token, "can't cast from " + nodeType.nodeType +
                                     " to " + type.nodeType);
            }

            // Can cast between some simple types.
            if (type.nodeType == Node.SIMPLE_TYPE)
            {
                if (type.typeCode != nodeType.typeCode)
                {
                    // They're different simple types.
                    var typeCode = type.typeCode;         // To Type
                    var nodeTypeCode = nodeType.typeCode; // From Type

                    if (typeCode == inst.defs.A || nodeTypeCode == inst.defs.A ||
                        typeCode == inst.defs.B || nodeTypeCode == inst.defs.B ||
                        typeCode == inst.defs.T || nodeTypeCode == inst.defs.T ||
                        typeCode == inst.defs.P || nodeTypeCode == inst.defs.P ||
                        typeCode == inst.defs.X || nodeTypeCode == inst.defs.X)
                    {

                        // These can't be cast.
                        throw new PascalError(this.token, "can't cast from " +
                                             inst.defs.typeCodeToName(nodeTypeCode) +
                                             " to " + inst.defs.typeCodeToName(typeCode));
                    }

                    // Cast Char to String, just return the same node.
                    if (typeCode == inst.defs.S && nodeTypeCode == inst.defs.C)
                    {
                        return this;
                    }

                    // Can always cast to a real.
                    if (typeCode == inst.defs.R ||
                        (typeCode == inst.defs.I && nodeTypeCode != inst.defs.R))
                    {

                        Node node = null;   //TODO: MVM
                                            //    var node = new Node(Node.CAST, type.token, {
                                            //    type: type,
                                            //    expression: this
                                            //                });
                                            //node.expressionType = type;
                        return node;
                    }

                    // Can't cast.
                    throw new PascalError(this.token, "can't cast from " +
                                         inst.defs.typeCodeToName(nodeTypeCode) +
                                         " to " + inst.defs.typeCodeToName(typeCode));
                }
                else
                {
                    // Same simple typeCode. If they're pointers, then they
                    // must be compatible types or the source must be nil.
                    if (type.typeCode == inst.defs.A)
                    {
                        if (nodeType.typeName == null)
                        {
                            // Assigning from Nil, always allowed.
                        }
                        else if (type.typeName == null)
                        {
                            // Assigning to generic pointer, always allowed.
                        }
                        else if (type.typeName.isSameIdentifier(nodeType.typeName))
                        {
                            // Same pointer type.
                        }
                        else
                        {
                            // Incompatible pointers, disallow. XXX test this.
                            throw new PascalError(this.token, "can't cast from pointer to " +
                                                  nodeType.print() + " to pointer to " + type.print());
                        }
                    }
                }
            }
            else
            {
                // Complex type. XXX We should verify that they're of the same type.
            }

            // Nothing to cast, return existing node.
            return this;
        }





    }
}
