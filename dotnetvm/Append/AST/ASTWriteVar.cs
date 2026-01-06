using Append.Types;

namespace Append.AST
{
    internal class ASTWriteVar(string VarName, ASTNode NewValue) : ASTNode
    {
        public string VarName { get; } = VarName;
        public Variable? Variable { get; set; }

        internal override TypeId KnownType => TypeId.None;

        internal override int SubNodeCount => 1;
        internal override ASTNode GetSubNode(int index)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            return NewValue;
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            NewValue = node;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            if (Variable == null)
                throw new Exceptions.UnresolvedVariableException();
            switch (step)
            {
                case 0:
                    step++;
                    return (ASTSignal.Enter, NewValue);

                default:
#if DEBUG
                    if (context.PeekFrame(Variable.StackIndex).TypeId != context.Value.TypeId)
                        throw new Exceptions.InvalidValueTypeException();
#endif
                    context.PokeFrame(Variable.StackIndex, context.Value);
                    return (ASTSignal.Done, null);
            }
        }

        internal override string ToString(int surroundingPriority)
            => $"{VarName} := {NewValue}";
    }
}