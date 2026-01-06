
namespace Append.AST
{
    public class ASTBlock(ASTNode[] Instructions) : ASTNode
    {
        internal override Types.TypeId KnownType => Instructions[^1].KnownType;
        internal override Types.TypeId ReturnType {
            get
            {
                foreach (ASTNode node in Instructions)
                {
                    var curType = node.ReturnType;
                    if (curType != Types.TypeId.None)
                        return curType;
                }
                return Types.TypeId.None;
            }
        }

        public ASTNode[] Instructions { get; } = Instructions;

        internal override int SubNodeCount => Instructions.Length;
        internal override ASTNode GetSubNode(int index)
        {
            return Instructions[index];
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            Instructions[index] = node;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            if (step < Instructions.Length)
            {
                var nextNode = Instructions[step];
                step++;
                if (nextNode is ASTFunction)
                    return (ASTSignal.Continue, null);
                else
                    return (ASTSignal.Enter, nextNode);
            }
            else
            {
#if DEBUG
                if (step > Instructions.Length)
                    throw new Exceptions.InvalidStepException();
#endif
                return (ASTSignal.Done, null);
            }
        }

        internal override string ToString(int surroundingPriority)
            => string.Join<ASTNode>(Environment.NewLine, Instructions);
    }
}
