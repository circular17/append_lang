using Append.Types;

namespace Append.AST
{
    public class ASTReadLocalVar(ASTLocalVar Variable) : ASTNode
    {
        internal override TypeId KnownType => Variable.TypeId;

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction) { }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode) { }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            context.Value = context.PeekFrame(Variable.ReverseStackIndex);
            return (ASTSignal.Done, null);
        }

        internal override string ToString(int surroundingPriority)
            => Variable.Name;
    }
}
