namespace Append.AST.Exceptions
{
    internal class NodeCannotBeReplacedException : Exception
    {
        public NodeCannotBeReplacedException() : base("Node cannot be replaced.") { }
    }
}
