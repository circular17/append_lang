
namespace Append.AST
{
    public abstract class ASTNode
    {
        internal abstract Types.TypeId KnownType { get; }
        internal virtual Types.TypeId ReturnType { get; } = Types.TypeId.None;
        public bool IsReturning { get; set; }

        internal abstract int SubNodeCount { get; }
        internal abstract ASTNode GetSubNode(int index);
        internal abstract void SetSubNode(int index, ASTNode node);

        internal abstract (ASTSignal, ASTNode?) Step(VMThread context, ref int step);

        internal abstract string ToString(int surroundingPriority);
        public override string ToString() => ToString(-1);
    }
}
