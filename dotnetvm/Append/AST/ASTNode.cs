
namespace Append.AST
{
    public abstract class ASTNode
    {
        internal abstract Types.TypeId KnownType { get; }
        internal virtual Types.TypeId ReturnType { get; } = Types.TypeId.None;
        public bool IsReturning { get; set; }

        internal abstract void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction);
        internal abstract void ReplaceSubNode(ASTNode oldNode, ASTNode newNode);

        internal abstract (ASTSignal, ASTNode?) Step(VMThread context, ref int step);

        internal abstract string ToString(int surroundingPriority);
        public override string ToString() => ToString(-1);
    }
}
