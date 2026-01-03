namespace Append.AST.Exceptions
{
    internal class DuplicateVariableNameException : Exception
    {
        public DuplicateVariableNameException() : base("Duplicate variable name.") { }
    }
}
