using Append.AST.Exceptions;
using Append.Types;

namespace Append.AST
{
    public class ASTReadVar(string VarName) : ASTNodeWithoutSubNodes
    {
        public string VarName { get; } = VarName;
        public Variable? Variable { get; set; }

        internal override TypeId KnownType => Variable?.TypeId ?? TypeId.None;

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            if (Variable == null)
                throw new UnresolvedVariableException();
            context.Value = context.PeekFrame(Variable.StackIndex);
            return (ASTSignal.Done, null);
        }

        internal override string ToString(int surroundingPriority)
            => VarName;
    }
}
