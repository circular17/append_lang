using Append.Parsing;

namespace Append.AST
{
    public class ASTBinaryIntrinsic(ASTNode LeftNode, ASTNode RightNode, BinaryIntrinsic Operation) : ASTNode
    {
        internal override Types.TypeId KnownType => Operation.ResultType;
        public BinaryIntrinsic Operation { get; } = Operation;

        internal override int SubNodeCount => 2;
        internal override ASTNode GetSubNode(int index)
        {
            return index switch
            {
                0 => LeftNode,
                1 => RightNode,
                _ => throw new IndexOutOfRangeException(nameof(index))
            };
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            switch (index)
            {
                case 0:
                    LeftNode = node;
                    break;

                case 1:
                    RightNode = node;
                    break;

                default:
                    throw new IndexOutOfRangeException(nameof(index));
            };
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            switch (step)
            {
                case 0:
                    step = 1;
                    return (ASTSignal.Enter, LeftNode);

                case 1:
                    context.PushValue();
                    step = 2;
                    return (ASTSignal.Enter, RightNode);

                default:
#if DEBUG
                    if (step != 2)
                        throw new Exceptions.InvalidStepException();
#endif
                    var rightValue = context.Value;
                    context.PopValue();
                    var leftValue = context.Value;

#if DEBUG
                    if (leftValue.TypeId != Operation.LeftType || rightValue.TypeId != Operation.RightType)
                        throw new Exceptions.InvalidValueTypeException();
#endif
                    context.Value = Operation.Apply(leftValue, rightValue);
                    return (ASTSignal.Done, null);
            }
        }

        internal override string ToString(int surroundingPriority)
        {
            var myPriority = Operations.Priorty(Operation.Name);
            return Operations.Brackets(
                $"{LeftNode.ToString(myPriority)} {Operation.Name} {RightNode.ToString(myPriority)}",
                myPriority, surroundingPriority);
        }
    }
}
