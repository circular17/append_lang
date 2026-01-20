using Append.Types;

namespace Append.AST
{
    public class ASTMain(ASTNode[] Instructions) : ASTVarContainer
    {
        internal override TypeId KnownType => Instructions[^1].KnownType;
        public ASTNode[] Instructions { get; } = Instructions;

        private readonly List<Variable> _locals = [];
        private readonly Dictionary<string, Variable> _varByName = [];

        public override Variable AddLocalVariable(string name, string typeName)
        {
            var variable = AddVariable(name, typeName);
            _locals.Add(variable);
            return variable;
        }

        private Variable AddVariable(string name, string typeName)
        {
            if (_varByName.ContainsKey(name))
                throw new Exceptions.DuplicateVariableNameException();

            var newVariable = new Variable(name, typeName)
            {
                StackIndex = _varByName.Count
            };
            _varByName.Add(name, newVariable);
            return newVariable;
        }

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
                if (step == 0)
                {
                    foreach (var variable in _locals)
                    {
                        context.Value = Value.DefaultFor(variable.TypeId);
                        context.PushValue();
                    }
                    context.EnterFrame(_locals.Count);
                }

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
                context.ExitFrame(_locals.Count);
                return (ASTSignal.Done, null);
            }
        }

        internal override string ToString(int surroundingPriority)
            => string.Join<ASTNode>(Environment.NewLine, Instructions);
    }
}
