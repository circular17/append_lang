using Append.Types;

namespace Append.AST
{
    internal class ASTDefineVar(string Name, string TypeName) : ASTNode
    {
        public string Name { get; } = Name;
        public string TypeName { get; } = TypeName;
        public ASTNode? InitialValue { get; set; }
        internal Variable? Variable { get; set; }

        internal override TypeId KnownType => TypeId.None; // a variable definition doesn't yield a value

        internal override int SubNodeCount => InitialValue == null ? 0 : 1;
        internal override ASTNode GetSubNode(int index)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            return InitialValue!;
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            InitialValue = node;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            if (Variable is null)
                throw new Exceptions.UnresolvedVariableException();
            switch (step)
            {
                case 0:
                    if (InitialValue == null)
                    {
                        context.Value = Value.DefaultFor(Variable.TypeId);
                        context.PokeFrame(Variable.StackIndex, context.Value);
                        return (ASTSignal.Done, null);
                    }
                    else
                    {
                        step++;
                        return (ASTSignal.Enter, InitialValue);
                    }

                default:
#if DEBUG
                    if (step != 1)
                        throw new Exceptions.InvalidStepException();
#endif
                    context.PokeFrame(Variable.StackIndex, context.Value);
                    return (ASTSignal.Done, null);
            }
        }

        internal override string ToString(int surroundingPriority)
        {
            return $"var {Name}: {TypeName}" +
                (InitialValue == null ? "" : $" = {InitialValue}");
        }
    }
}
