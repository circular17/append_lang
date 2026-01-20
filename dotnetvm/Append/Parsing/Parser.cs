using Append.AST;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Append.Parsing
{
    public class Parser(string text)
    {
        public string Text { get; } = text;
        public List<Token> Tokens { get; } = Tokenizer.Tokenize(text);
        private int _index;

        public ASTNode Parse()
        {
            _index = 0;
            return ParseProgram();
        }

        private ASTMain ParseProgram()
        {
            List<ASTNode> instructions = [];
            SkipComments();
            while (!Finished)
            {
                if (TryInstructionOrDefinition(out var instruction))
                    instructions.Add(instruction);
                else
                    throw UnexpectedToken();
                SkipEOL();
            }
            return new ASTMain([.. instructions]);
        }

        private Exception UnexpectedToken()
        {
            if (Finished)
                return new UnexpectedEndOfFileException();
            else
                return new UnexpectedTokenException(Tokens[_index]);
        }

        bool Finished => _index == Tokens.Count;

        private void SkipComments()
        {
            while (_index < Tokens.Count && Tokens[_index].Kind == TokenKind.Comment)
                _index++;
        }

        private void SkipEOL()
        {
            while (_index < Tokens.Count && Tokens[_index].Kind == TokenKind.EndOfLine)
                _index++;
        }

        private bool TryInstructionOrDefinition([NotNullWhen(true)] out ASTNode? instruction)
        {
            if (TryFunction(out ASTFunction? function))
            {
                instruction = function;
                return true;
            }
            else if (TryVarDef(out ASTDefineVar? def))
            {
                instruction = def;
                return true;
            }
            else
                return TryInstruction(out instruction);
        }

        private bool TryInstruction([NotNullWhen(true)] out ASTNode? instruction)
        {
            if (TryExpression(out ASTNode? expr))
            {
                if (TryKeyword("loop"))
                {
                    if (!TryBlockOrInstruction(out var body))
                        throw new InstructionExpectedException();
                    instruction = new ASTLoop(expr, body);
                }
                else
                    instruction = expr;
                return true;
            }
            else if (TryKeyword("loop"))
            {
                if (!TryBlockOrInstruction(out var body))
                    throw new InstructionExpectedException();
                instruction = new ASTLoop(new ASTConst(Value.FromBool(true)), body);
                return true;
            }

            instruction = null;
            return false;
        }

        private bool TryBlockOrInstruction([NotNullWhen(true)] out ASTNode? instruction)
        {
            if (TryBlock(out var block))
            {
                instruction = block;
                return true;
            }
            else
                return TryInstruction(out instruction);
        }

        private bool TryVarDef([NotNullWhen(true)] out ASTDefineVar? def)
        {
            if (TryKeyword("var"))
            {
                if (!TryIdentifier(out var varName))
                    throw new ExpectedTokenException(TokenKind.Identifier);
                ExpectToken(TokenKind.Colon);
                if (!TryIdentifier(out var typeName))
                    throw new ExpectedTokenException(TokenKind.Identifier);
                def = new ASTDefineVar(varName, typeName);
                if (TryToken(TokenKind.Operator, "="))
                {
                    if (!TryExpression(out ASTNode? expr))
                        throw new ExpressionExpectedException();
                    def.InitialValue = expr;
                }
                return true;
            }
            else
            {
                def = null;
                return false;
            }
        }

        private bool TryFunction([NotNullWhen(true)] out ASTFunction? function)
        {
            if (TryKeyword("func"))
            {
                List<(string Name, string TypeName)> parameters = [];
                if (ParseVariableAndType(out var firstParam))
                    parameters.Add(firstParam);
                if (TryToken(TokenKind.SingleQuote))
                {
                    if (!TryIdentifier(out var funcName) &&
                        !TryOperator(out funcName))
                        throw new FunctionNameExpected();
                    ExpectToken(TokenKind.SingleQuote);
                    if (ParseVariableAndType(out var nextParam))
                    {
                        parameters.Add(nextParam);
                        while (TryToken(TokenKind.Semicolon))
                        {
                            if (ParseVariableAndType(out nextParam))
                                parameters.Add(nextParam);
                        }
                    }
                    function = new ASTFunction(funcName,
                        parameters: [.. parameters]);
                    SkipEOL();
                    if (TryToken(TokenKind.DoubleArrow))
                    {
                        if (!TryExpression(out var body))
                            throw new ExpressionExpectedException();
                        function.Body = body;
                    }
                    else
                    {
                        if (!TryBlock(out var body))
                            throw new FunctionBodyExpected();
                        function.Body = body;
                    }
                    return true;
                }
                else
                    throw new FunctionNameExpected();
            }
            else
            {
                function = null;
                return false;
            }
        }

        private bool TryBlock([NotNullWhen(true)] out ASTBlock? block)
        {
            if (!TryToken(TokenKind.OpenBrace))
            {
                block = null;
                return false;
            }
            SkipEOL();
            List<ASTNode> instructions = [];
            while (!TryToken(TokenKind.CloseBrace))
            {
                if (!TryInstructionOrDefinition(out var instruction))
                    throw UnexpectedToken();
                instructions.Add(instruction);
                SkipEOL();
            }
            block = new ASTBlock([.. instructions]);
            return true;
        }

        private bool TryKeyword(string keyword)
        {
            if (_index < Tokens.Count && Tokens[_index].Kind == TokenKind.Keyword
                && Tokens[_index].ToString() == keyword)
            {
                _index++;
                return true;
            }
            else
                return false;
        }

        private bool ParseVariableAndType(out (string Name, string TypeName) variable)
        {
            if (TryIdentifier(out var varName))
            {
                if (TryToken(TokenKind.Colon))
                {
                    if (TryIdentifier(out var varType))
                    {
                        variable = (varName, varType);
                        return true;
                    }
                    else
                        throw new MissingTypeException();
                }
                else
                {
                    variable = (varName, "");
                    return true;
                }
            }
            else
            {
                variable = ("", "");
                return false;
            }
        }

        private bool TryIdentifier([NotNullWhen(true)] out string? identifier)
        {
            if (_index < Tokens.Count && Tokens[_index].Kind == TokenKind.Identifier)
            {
                identifier = Tokens[_index].ToString();
                _index++;
                return true;
            }
            else
            {
                identifier = null;
                return false;
            }
        }

        private bool TryOperator([NotNullWhen(true)] out string? opName,
            string[]? possibilities = null)
        {
            if (_index < Tokens.Count &&
                Tokens[_index].Kind == TokenKind.Operator)
            {
                opName = Tokens[_index].ToString();
                if (possibilities == null ||
                    possibilities.Contains(opName))
                {
                    _index++;
                    return true;
                }
            }
            opName = null;
            return false;
        }

        private bool TryToken(TokenKind kind)
        {
            if (_index < Tokens.Count && Tokens[_index].Kind == kind)
            {
                _index++;
                return true;
            }
            else
                return false;
        }

        private bool TryToken(TokenKind kind, string tokenName)
        {
            if (_index < Tokens.Count && Tokens[_index].Kind == kind
                && Tokens[_index].ToString() == tokenName)
            {
                _index++;
                return true;
            }
            else
                return false;
        }

        private bool TryToken(TokenKind kind, out string tokenName)
        {
            if (_index < Tokens.Count && Tokens[_index].Kind == kind)
            {
                tokenName = Tokens[_index].ToString();
                _index++;
                return true;
            }
            else
            {
                tokenName = "";
                return false;
            }
        }

        private void ExpectToken(TokenKind kind)
        {
            if (_index < Tokens.Count && Tokens[_index].Kind == kind)
            {
                _index++;
                return;
            }
            else
                throw new ExpectedTokenException(kind);
        }

        private bool TryExpression([NotNullWhen(true)] out ASTNode? expr)
        {
            return TryAssignment(out expr);
        }

        private bool TryAssignment([NotNullWhen(true)] out ASTNode? expr)
        {
            if (TryTernary(out var leftOperand))
            {
                if (TryToken(TokenKind.Assignment, ":="))
                {
                    if (leftOperand is ASTReadVar read)
                    {
                        if (!TryExpression(out var assignedValue))
                            throw new ExpressionExpectedException();
                        expr = new ASTWriteVar(read.VarName, assignedValue);
                        return true;
                    }
                    else
                        throw new LeftSideCannotBeAssignedException();
                }
                else
                {
                    expr = leftOperand;
                    return true;
                }
            }
            else
            {
                expr = null;
                return false;
            }
        }

        private bool TryTernary([NotNullWhen(true)] out ASTNode? expr)
        {
            if (TryComparison(out var leftOperand))
            {
                if (TryToken(TokenKind.Operator, "?"))
                {
                    if (TryToken(TokenKind.EndOfLine))
                    {
                        if (!TryBlockOrInstruction(out var yesBlock))
                            throw new InstructionExpectedException();
                        SkipEOL();
                        if (TryKeyword("else"))
                        {
                            SkipEOL();
                            if (!TryBlockOrInstruction(out var noBlock))
                                throw new InstructionExpectedException();
                            expr = new ASTIfElse(leftOperand, yesBlock, noBlock);
                            return true;
                        }
                        else
                        {
                            expr = new ASTIf(leftOperand, yesBlock);
                            return true;
                        }
                    }
                    else
                    {
                        if (!TryTernary(out var ifYes))
                            throw new ExpressionExpectedException();
                        SkipEOL();
                        if (TryKeyword("else"))
                        {
                            if (!TryTernary(out var ifNo))
                                throw new ExpressionExpectedException();
                            expr = new ASTIfElse(leftOperand, ifYes, ifNo);
                            return true;
                        }
                        else
                        {
                            expr = new ASTIf(leftOperand, ifYes);
                            return true;
                        }
                    }
                }
                else
                {
                    expr = leftOperand;
                    return true;
                }
            }
            else
            {
                expr = null;
                return false;
            }
        }
        

        delegate bool TryLevelHandler([NotNullWhen(true)] out ASTNode? expr);

        private readonly string[] _comparisonOperators = Operations.Comparison.Split(' ');
        private bool TryComparison([NotNullWhen(true)] out ASTNode? expr)
        {
            return TryBinaryOperator(out expr, _comparisonOperators, TryAddition);
        }

        private readonly string[] _additionOperators = Operations.Addition.Split(' ');
        private bool TryAddition([NotNullWhen(true)] out ASTNode? expr)
        {
            return TryBinaryOperator(out expr, _additionOperators, TryMultiplication);
        }

        private readonly string[] _multiplicationOperators = Operations.Multiplication.Split(' ');
        private bool TryMultiplication([NotNullWhen(true)] out ASTNode? expr)
        {
            return TryBinaryOperator(out expr, _multiplicationOperators, TryAtom);
        }

        private bool TryBinaryOperator([NotNullWhen(true)] out ASTNode? expr,
            string[] operators, TryLevelHandler nextLevel)
        {
            if (!nextLevel(out ASTNode? leftOperand))
            {
                expr = null;
                return false;
            }
            while (TryOperator(out var opName, operators))
            {
                if (!nextLevel(out ASTNode? rightOperand))
                    throw new RightOperandExpectedException();
                leftOperand = new ASTCall(opName, [leftOperand, rightOperand]);
            }
            expr = leftOperand;
            return true;
        }

        private bool TryAtom([NotNullWhen(true)] out ASTNode? expr)
        {
            if (TryToken(TokenKind.OpenBracket))
            {
                if (!TryExpression(out expr))
                    throw new ExpressionExpectedException();
                ExpectToken(TokenKind.CloseBracket);
                return true;
            }
            else if (TryToken(TokenKind.Number, out string numberStr))
            {
                if (long.TryParse(numberStr, CultureInfo.InvariantCulture, out var longValue))
                {
                    expr = new ASTConst(Value.FromInt(longValue));
                    return true;
                }
                if (double.TryParse(numberStr, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    expr = new ASTConst(Value.FromFloat(doubleValue));
                    return true;
                }
                throw new InvalidNumberException();
            }
            else if (TryToken(TokenKind.Identifier, out string varName))
            {
                expr = new ASTReadVar(varName);
                return true;
            }
            else if (TryToken(TokenKind.Void))
            {
                expr = new ASTConst(Value.Void);
                return true;
            }
            expr = null;
            return false;
        }
    }
}
