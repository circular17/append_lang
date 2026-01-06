namespace Append
{
    internal class UnexpectedParentScopeException : Exception
    {
        public UnexpectedParentScopeException() : base("Unexpected parent scope.") { }
    }
}
