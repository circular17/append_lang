
namespace Append.AST
{
    public class ASTBreak : ASTNode
    {
        internal override Types.TypeId KnownType => Types.TypeId.None;
        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction) { }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode) { }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            return (ASTSignal.Break, null);
        }

        internal override string ToString(int surroundingPriority)
            => "break";
    }
}
