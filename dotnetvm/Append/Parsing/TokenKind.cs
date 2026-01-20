namespace Append.Parsing
{
    public enum TokenKind
    {
        Unknown,
        Number,
        Operator,
        Identifier,
        Keyword,
        Comment,          // "// ..." or "/* ... */"
        SingleQuote,
        EndOfLine,
        Void,             // ()
        OpenBrace,        // {
        CloseBrace,       // }
        OpenBracket,      // (, [
        CloseBracket,     // ), ]
        OpenWideBracket,  // {|, [|
        CloseWideBracket, // |}, |]   
        Pipe,             // |
        DoubleArrow,      // =>
        Assignment,
        Dot,              // .
        Colon,            // :
        Semicolon,        // ;
        SingleArrow,      // ->
        ThreeDots,        // ...
        EmptyLinkedList,  // <>
    }
}
