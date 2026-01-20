namespace Append.Parsing
{
    public record class Token(TokenKind Kind, int Start, int Length, string Code)
    {
        public override string ToString()
        {
            return Code.Substring(Start, Length);
        }
    }
}
