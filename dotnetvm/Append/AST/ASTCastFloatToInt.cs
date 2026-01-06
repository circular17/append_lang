
using Append.Parsing;

namespace Append.AST
{
    public class ASTCastFloatToInt(ASTNode SubNode) : ASTNode
    {
        internal override Types.TypeId KnownType => Types.TypeId.Int;

        internal override int SubNodeCount => 1;
        internal override ASTNode GetSubNode(int index)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            return SubNode!;
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            SubNode = node;
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
