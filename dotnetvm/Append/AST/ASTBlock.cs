
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

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            for (int i = 0; i < Instructions.Length; i++)
                Instructions[i] = replaceFunction(this, Instructions[i]);
        }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            for (int i = 0; i < Instructions.Length; i++)
                if (oldNode == Instructions[i])
                    Instructions[i] = oldNode;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            if (step < Instructions.Length)
            {
                var nextNode = Instructions[step];
                step++;
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
