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

        internal override int SubNodeCount => 1;
        internal override ASTNode GetSubNode(int index)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            return Result;
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            _result = node;
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
