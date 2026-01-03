using Append.Types;

namespace Append.AST
{
    public class ASTReturn : ASTNode
    {
        private ASTNode _result;
        public ASTNode Result => _result;

        public ASTReturn(ASTNode result)
        {
            _result = result;
            _result.IsReturning = true;
        }

        internal override TypeId KnownType => TypeId.None;
        internal override TypeId ReturnType => _result.KnownType;

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            _result = replaceFunction(this, _result);
        }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            if (oldNode == _result)
                _result = oldNode;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            switch (step)
            {
                case 0:
                    step++;
                    return (ASTSignal.Enter, _result);
                
                default:
                    return (ASTSignal.Return, null);
            }
        }

        internal override string ToString(int surroundingPriority)
            => $"return {_result}";
    }
}
