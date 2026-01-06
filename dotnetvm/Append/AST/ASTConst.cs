using Append.Types;

namespace Append.AST
{
    public class ASTConst(Value Value) : ASTNodeWithoutSubNodes
    {
        internal override TypeId KnownType => Value.TypeId;

        public Value Value { get; } = Value;

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            context.Value = Value;
            return (ASTSignal.Done, null);
        }

        internal override string ToString(int surroundingPriority) => Value.ToString();
    }
}
