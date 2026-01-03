
using Append.Parsing;

namespace Append.AST
{
    public class ASTCastFloatToInt(ASTNode SubNode) : ASTNode
    {
        internal override Types.TypeId KnownType => Types.TypeId.Int;

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            SubNode = replaceFunction(this,SubNode);
        }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            if (oldNode == SubNode)
                SubNode = newNode;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            switch (step)
            {
                case 0:
                    step = 1;
                    return (ASTSignal.Enter, SubNode);

                case 1:
#if DEBUG
                    if (context.Value.TypeId != Types.TypeId.Float)
                        throw new Exceptions.InvalidValueTypeException();
#endif 
                    var floatValue = context.Value.Data.Float;
                    if (floatValue < long.MinValue)
                        context.Value = Value.FromInt(long.MinValue);
                    else if (floatValue > long.MaxValue)
                        context.Value = Value.FromInt(long.MaxValue);
                    else if (double.IsNaN(floatValue))
                        context.Value = Value.FromInt(0L);
                    else
                        context.Value = Value.FromInt((long)floatValue);
                    return (ASTSignal.Done, null);

                default:
                    throw new Exceptions.InvalidStepException();
            }
        }

        internal override string ToString(int surroundingPriority)
        {
            var myPriority = Operations.Priorty(Operations.FunctionCall);
            return Operations.Brackets($"{SubNode.ToString(myPriority)} int",
                myPriority,
                surroundingPriority);
        }
           
    }
}
