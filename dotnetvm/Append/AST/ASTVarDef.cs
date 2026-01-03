using Append.Types;

namespace Append.AST
{
    internal class ASTVarDef(Value[] InitialValues, ASTNode Block) : ASTNode
    {
        internal override TypeId KnownType => Block.KnownType;

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            Block = replaceFunction(this, Block);
        }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            if (oldNode == Block)
                Block = newNode;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            switch (step)
            {
                case 0:
                    foreach (Value value in InitialValues)
                    {
                        context.Value = value;
                        context.PushValue();
                    }
                    context.EnterFrame(InitialValues.Length);
                    step++;
                    return (ASTSignal.Enter, Block);

                default:
#if DEBUG
                    if (step != 1)
                        throw new Exceptions.InvalidStepException();
#endif

                    context.ExitFrame(InitialValues.Length);
                    return (ASTSignal.Done, null);
            }
        }

        internal override string ToString(int surroundingPriority)
            => Block.ToString(surroundingPriority);
    }
}
