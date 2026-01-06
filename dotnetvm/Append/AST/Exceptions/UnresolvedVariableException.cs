namespace Append.AST.Exceptions
{
    internal class UnresolvedVariableException : Exception
    {
        public UnresolvedVariableException() : base("Unresolved variable.") { }
    }
}
