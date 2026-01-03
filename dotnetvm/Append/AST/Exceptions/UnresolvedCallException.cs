namespace Append.AST.Exceptions
{
    internal class UnresolvedCallException : Exception
    {
        public UnresolvedCallException() : base("Unresolved call.") { }
    }
}
