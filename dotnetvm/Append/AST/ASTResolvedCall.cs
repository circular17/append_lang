using Append.Parsing;
using Append.Types;

namespace Append.AST
{
    public class ASTResolvedCall : ASTNode
    {
        private readonly ASTFunction _function;
        public ASTFunction Function => _function;

        private readonly ASTNode[] _parameters;

        public ASTResolvedCall(ASTFunction Function, ASTNode[] Parameters)
        {
            _function = Function;
            _parameters = Parameters;
            if (Parameters.Length != Function.ParameterCount)
                throw new ArgumentException("Parameter count do not match function declaration");
        }

        internal override TypeId KnownType => _function.KnownType;

        internal override int SubNodeCount => _parameters.Length;
        internal override ASTNode GetSubNode(int index)
        {
            if (index < _parameters.Length)
                return _parameters[index];
            else 
                throw new ArgumentOutOfRangeException(nameof(index));
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            if (index < _parameters.Length)
                _parameters[index] = node;
            else
                throw new ArgumentOutOfRangeException(nameof(index));
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
            else if (step == _parameters.Length)
            {
                context.PushValue(); // push last parameter
                context.EnterFrame(_parameters.Length);
                step++;
                context.PushUnwind(this,returnStep: step);
                return (ASTSignal.Enter, _function);
            }
            else
            {
#if DEBUG
                if (step > _parameters.Length + 1)
                    throw new Exceptions.InvalidStepException();
#endif
                context.PopUnwind(this);
                context.ExitFrame(_parameters.Length);
                return (ASTSignal.Done, null);
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
