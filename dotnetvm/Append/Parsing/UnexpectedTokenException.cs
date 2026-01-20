namespace Append.Parsing
{
    internal class UnexpectedTokenException(Token token) 
        : Exception(token.Kind == TokenKind.EndOfLine ? 
            "Unexpected end of line" :
            $"Unexpected token '{token}'")
    {
    }
}
