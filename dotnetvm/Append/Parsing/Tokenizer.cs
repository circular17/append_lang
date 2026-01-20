namespace Append.Parsing
{
    internal class Tokenizer
    {
        private static bool IsWhitespace(char c)
            => c == ' ' || c == '\t';

        public static List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            int pos = 0;
            while (pos < input.Length)
            {
                while (pos < input.Length && IsWhitespace(input[pos]))
                    pos++;
                if (pos == input.Length)
                    break;

                char c = input[pos];
                if (IsDigit(c) || 
                    (IsSign(c) || c == '.') && pos + 1 < input.Length 
                    && IsDigit(input[pos + 1]))
                {
                    tokens.Add(NumberToken(input, ref pos));
                    continue;
                }
                else if (c == '/' && pos + 1 < input.Length && 
                    (input[pos + 1] == '/' || input[pos + 1] == '*'))
                {
                    tokens.Add(CommentToken(input, ref pos));
                    continue;
                }
                else if (Operations.IsOperatorChar(c))
                {
                    tokens.Add(OperatorToken(input, ref pos));
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    tokens.Add(IdentifierToken(input, ref pos));
                }
                else
                {
                    pos++;
                    tokens.Add(new Token(TokenKind.Unknown, pos - 1, 1, input));
                }
            }
            return tokens;
        }

        private static bool IsDigit(char c)
            => c >= '0' && c <= '9';

        private static bool IsE(char c)
            => c == 'e' || c == 'E';

        private static bool IsSign(char c)
            => c == '+' || c == '-';

        public static Token NumberToken(string input, ref int pos)
        {
            int startPos = pos;
            while (pos < input.Length && IsDigit(input[pos])) pos++;
            if (pos < input.Length && input[pos] == '.')
            {
                pos++;
                while (pos < input.Length && IsDigit(input[pos])) pos++;
            }
            if (pos < input.Length && IsE(input[pos]))
            {
                pos++;
                if (pos < input.Length && IsSign(input[pos])) pos++;
                while (pos < input.Length && IsDigit(input[pos])) pos++;
            }
            return new Token(TokenKind.Number, startPos, pos - startPos, input);
        }

        private static bool IsLineEndingChar(char c)
        {
            return c == 10 || c == 13;
        }

        private static Token CommentToken(string input, ref int pos)
        {
            int startPos = pos;
            pos += 2;
            if (input[pos-1] == '/')
            {
                while (pos < input.Length && !IsLineEndingChar(input[pos]))
                    pos++;
            }
            else
            {
                while (pos + 1 < input.Length && 
                    (input[pos] != '*' || input[pos + 1] != '/'))
                    pos++;
                if (pos == input.Length - 1)
                    pos++;
            }
            return new Token(TokenKind.Comment, startPos, pos - startPos, input);
        }

        private static bool IsIdentifierChar(char c)
            => c == '_' || char.IsLetter(c) || char.IsDigit(c);

        public static readonly string[] Keywords = 
            ["func", "var", "loop", "else"];

        private static Token IdentifierToken(string input, ref int pos)
        {
            int startPos = pos;
            while (pos < input.Length && IsIdentifierChar(input[pos]))
                pos++;

            var word = input[startPos..pos];
            if (Keywords.Contains(word))
                return new Token(TokenKind.Keyword, startPos, pos - startPos, input);
            else
                return new Token(TokenKind.Identifier, startPos, pos - startPos, input);
        }

        private static Token OperatorToken(string input, ref int pos)
        {
            var startPos = pos;
            var kind = FindOperatorTokenKind(input, ref pos);
            return new Token(kind, startPos, pos - startPos, input);
        }

        private static TokenKind FindOperatorTokenKind(string input, ref int pos)
        {
            var c = input[pos];
            pos++;
            char c2 = pos < input.Length ? input[pos] : '\0';
            if (c == '(')
            {
                if (c2 == ')')
                {
                    pos++;
                    return TokenKind.Void;
                }
                else
                    return TokenKind.OpenBracket;
            }
            else if (c == ')')
                return TokenKind.CloseBracket;
            else if (c == '|')
            {
                if (c2 == ']' && c2 == '}')
                {
                    pos++;
                    return TokenKind.CloseWideBracket;
                }
                else
                    return TokenKind.Pipe;

            }
            else if (c == '}')
            {
                return TokenKind.CloseBrace;
            }
            else if (c == '[' || c == '{')
            {
                if (c2 == '|')
                {
                    pos++;
                    return TokenKind.OpenWideBracket;
                }
                else if (c == '{')
                    return TokenKind.OpenBrace;
                else
                    return TokenKind.OpenBracket;
            }
            else if (c == '=')
            {
                if (c2 == '>')
                {
                    pos++;
                    return TokenKind.DoubleArrow;
                }
                // equality "="
                else
                    return TokenKind.Operator;
            }
            else if (c == '+')
            {
                if (c2 == '+')
                {
                    // concatenation "++"
                    pos++;
                }
                if (pos < input.Length && input[pos] == '=')
                {
                    pos++;
                    return TokenKind.Assignment;
                }
                else
                    return TokenKind.Operator;
            }
            else if (c == '?') // ternary
            {
                if (c2 == '?')
                {
                    // coalescing "??"
                    pos++;
                }
                return TokenKind.Operator;
            }
            else if (c == '!') // bit negation
            {
                if (c2 == '=')
                {
                    // difference "!="
                    pos++;
                }
                return TokenKind.Operator;
            }
            else if (c == '>')
            {
                if (c2 == '>')
                {
                    // composition ">>"
                    pos++;
                }
                return TokenKind.Operator;
            }
            else if (c == '.')
            {
                if (c2 == '.')
                {
                    // range ".."
                    pos++;

                    if (pos < input.Length && input[pos] == '.') // splat "..."
                    {
                        pos++;
                        return TokenKind.ThreeDots;
                    }

                    if (pos < input.Length && input[pos] == '=') // inclusive "..="
                        pos++;
                    return TokenKind.Operator;
                }
                else
                    return TokenKind.Dot;

            }
            else if (c == '-')
            {
                if (c2 == '>')
                {
                    pos++;
                    return TokenKind.SingleArrow;
                }
                else if (c2 == '=')
                {
                    pos++;
                    return TokenKind.Assignment;
                }
                else
                    return TokenKind.Operator;
            }
            else if (c == '<')
            {
                if (c2 == '>')
                {
                    pos++;
                    return TokenKind.EmptyLinkedList;
                }
                else if (c2 == '<' || c2 == '=')
                {
                    // reverse composition "<<"
                    // lower than or equal "<="
                    pos++;
                }
                return TokenKind.Operator;
            }
            else if (c == ':')
            {
                if (c2 == '=')
                {
                    pos++;
                    return TokenKind.Assignment;
                }
                else
                    return TokenKind.Colon;
            }
            else if (c == ';')
                return TokenKind.Semicolon;
            else if (c == '\'')
                return TokenKind.SingleQuote;
            else if (c == '\r')
            {
                if (c2 == '\n')
                    pos++;
                return TokenKind.EndOfLine;
            }
            else
            {
                if (c2 == '=')
                {
                    pos++;
                    return TokenKind.Assignment;
                }
                else
                    return TokenKind.Operator;
            }
        }
    }
}
