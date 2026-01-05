using Append.Parsing;
using Append.Types;

namespace Append.AST
{
    public class ASTTailCall : ASTNode
    {
        private readonly ASTFunction _function;
        private readonly ASTNode[] _parameters;

        public ASTTailCall(ASTFunction Function, ASTNode[] Parameters)
        {
            _function = Function;
            _parameters = Parameters;
            if (Parameters.Length != Function.ParameterCount)
                throw new ArgumentException("Parameter count do not match function declaration");
        }

        internal override TypeId KnownType => _function.KnownType;

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            for (int i = 0; i < _parameters.Length; i++)
                _parameters[i] = replaceFunction(this, _parameters[i]);
        }

        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            for (int i = 0; i < _parameters.Length; i++)
                if (oldNode == _parameters[i])
                    _parameters[i] = newNode;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            if (step < _parameters.Length)
            {
                if (step > 0)
                    context.PushValue(); // push previous parameter
                var nextNode = _parameters[step];
                step++;
                return (ASTSignal.Enter, nextNode);
            }
            else 
            {
#if DEBUG
                if (step > _parameters.Length)
                    throw new Exceptions.InvalidStepException();
#endif
                context.PushValue(); // push last parameter
                return (ASTSignal.TailCall, _function);
            }
        }

        internal override string ToString(int surroundingPriority)
        {
            var myPriority = Operations.Priorty(_function.Name);
            string span;
            if (_parameters.Length == 0)
                span = $"() {_function.Name}";
            else if (_parameters.Length == 1)
                span = $"{_parameters[0].ToString(myPriority)} {_function.Name}";
            else
                span = $"{_parameters[0].ToString(myPriority)} {_function.Name} " 
                    + string.Join("; ", _parameters.Skip(1).Select(p => p.ToString(myPriority)));

            return Operations.Brackets(
                span, myPriority, surroundingPriority);
        }
    }
}
