using Append.Types;

namespace Append.AST
{
    internal class ASTWriteVar(ASTLocalVar Variable, ASTNode NewValue) : ASTNode
    {
        internal override TypeId KnownType => TypeId.None;

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            NewValue = replaceFunction(this, NewValue);
        }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            if (oldNode == NewValue)
                NewValue = oldNode;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            switch (step)
            {
                case 0:
                    step++;
                    return (ASTSignal.Enter, NewValue);

                default:
#if DEBUG
                    if (context.PeekFrame(Variable.ReverseStackIndex).TypeId != context.Value.TypeId)
                        throw new Exceptions.InvalidValueTypeException();
#endif
                    context.PokeFrame(Variable.ReverseStackIndex, context.Value);
                    return (ASTSignal.Done, null);
            }
        }

        internal override string ToString(int surroundingPriority)
            => $"{NewValue} -> {Variable.Name}";
    }
}