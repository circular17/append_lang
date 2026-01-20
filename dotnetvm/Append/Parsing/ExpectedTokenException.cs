namespace Append.Parsing
{
    internal class ExpectedTokenException(TokenKind token) 
        : Exception($"Expecting {TokenName(token)} token.")
    {
        public static string TokenName(TokenKind kind)
        {
            return kind switch
            {
                TokenKind.Unknown => "unknown",
                TokenKind.Number => "number",
                TokenKind.Operator => "operator",
                TokenKind.Identifier => "identifer",
                TokenKind.Keyword => "keyword",
                TokenKind.Comment => "comment",
                TokenKind.SingleQuote => "single quote",
                TokenKind.EndOfLine => "end of line",
                TokenKind.Void => "()",
                TokenKind.OpenBrace => "{",
                TokenKind.CloseBrace => "}",
                TokenKind.OpenBracket => "'(' or '['",
                TokenKind.CloseBracket => "')' or ']'",
                TokenKind.OpenWideBracket => "'{[' or '[|'",
                TokenKind.CloseWideBracket => "'|}' or '|]'",
                TokenKind.Pipe => "|",
                TokenKind.DoubleArrow => "=>",
                TokenKind.Assignment => "assignment",
                TokenKind.Dot => ".",
                TokenKind.Colon => ":",
                TokenKind.Semicolon => ";",
                TokenKind.SingleArrow => "->",
                TokenKind.ThreeDots => "...",
                TokenKind.EmptyLinkedList => "<>",
                _ => $"<{(int)kind}>"
            };
        }
    }
}
