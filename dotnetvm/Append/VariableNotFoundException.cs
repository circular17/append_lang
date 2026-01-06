namespace Append
{
    internal class VariableNotFoundException : Exception
    {
        public VariableNotFoundException() : base("Variable not found.") { }
    }
}
