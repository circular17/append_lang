using Append.Types;

namespace Append.AST
{
    public class ASTFunction : ASTVarContainer
    {
        public string Name { get; }

        private readonly List<Variable> _parameters = [];
        private readonly List<Variable> _locals = [];
        private readonly Dictionary<string, Variable> _varByName = [];

        private ASTNode? _body;
        public ASTNode? Body
        {
            get => _body;
            set {
                if (_body != value) {
                    if (_body != null)
                        DetectReturningNodes(_body, false);
                    _body = value;
                    if (_body != null)
                        DetectReturningNodes(_body, true);
                }
            }
        }
        public bool IsIntrinsic { get; set; }

        public ASTFunction(string name, 
            (string Name, string TypeName)[]? parameters = null,
            ASTNode? body = null)
        {
            Name = name;
            Body = body;
            if (parameters != null)
                foreach (var param in parameters.Reverse())
                    AddParameterVariable(param.Name, param.TypeName);
        }

        public void AddParameterVariable(string name, string typeName)
        {
            var variable = AddVariable(name, typeName);
            _parameters.Add(variable);
        }

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

            foreach (var existingVariable in _varByName.Values)
                existingVariable.StackIndex--;

            var newVariable = new Variable(name, typeName)
            {
                StackIndex = -1
            };
            _varByName.Add(name, newVariable);
            return newVariable;
        }

        private static void DetectReturningNodes(ASTNode root, bool setFlag)
        {
            if (root is ASTBlock block && block.Instructions.Length > 0)
                DetectReturningNodes(block.Instructions[^1], setFlag);
            else if (root is ASTIfElse ifElse)
            {
                DetectReturningNodes(ifElse.YesBody, setFlag);
                DetectReturningNodes(ifElse.NoBody, setFlag);
            }
            else if (root is ASTReturn ret)
                DetectReturningNodes(ret.Result, setFlag);
            else
                root.IsReturning = setFlag;
        }

        public int ParameterCount => _parameters.Count;
        public Variable GetParameter(int index)
            => _parameters[index];

        public TypeId[] KnownParameterTypes =>
            [.. from p in _parameters select p.TypeId];

        private TypeId _knownType = TypeId.None;
        internal override TypeId KnownType
        {
            get
            {
                if (_knownType == TypeId.None && Body != null)
                {
                    _knownType = Body.ReturnType;
                    if (_knownType == TypeId.None)
                    {
                        _knownType = Body.KnownType;
                    }
                }
                return _knownType;
            }
        } 

        internal override int SubNodeCount => Body == null ? 0 : 1;
        internal override ASTNode GetSubNode(int index)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            return Body!;
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            Body = node;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            if (Body == null)
                throw new Exceptions.AbstractFunctionException();
            if (_locals.Count == 0)
                return (ASTSignal.Jump, Body);
            else
            {
                switch (step)
                {
                    case 0:
                        foreach (var variable in _locals)
                        {
                            context.Value = Value.DefaultFor(variable.TypeId);
                            context.PushValue();
                        }
                        context.EnterFrame(_locals.Count);
                        step++;
                        return (ASTSignal.Enter, Body);

                    default:
#if DEBUG
                        if (step != 1)
                            throw new Exceptions.InvalidStepException();
#endif
                        context.ExitFrame(_locals.Count);
                        return (ASTSignal.Done, null);
                }
            }
        }

        internal bool SameParameterTypes(TypeId[] parameterTypeIds)
        {
            if (parameterTypeIds.Length != ParameterCount)
                return false;
            for (int i = 0; i < ParameterCount; i++)
            {
                if (parameterTypeIds[i] != _parameters[i].TypeId
                    || _parameters[i].TypeId == TypeId.None)
                    return false;
            }
            return true;
        }

        internal bool SameParameterTypes(ASTFunction function)
            => SameParameterTypes(function.KnownParameterTypes);

        internal override string ToString(int surroundingPriority)
        {
            string header;
            if (_parameters.Count == 0)
                header = $"func '{Name}'";
            else if (_parameters.Count == 1)
                header = $"func {_parameters[0]} '{Name}'";
            else
                header = $"func {_parameters[0]} '{Name}' {string.Join("; ", _parameters.Skip(1))}";

            if (Body is ASTBlock)
            {
                return header + " {" + Environment.NewLine + 
                    Parsing.Formatting.Indent(Body?.ToString() ?? VoidTypeDef.VoidLitteral) +
                    Environment.NewLine + "}";
            }
            else
            {
                var bodyStr = Body?.ToString() ?? VoidTypeDef.VoidLitteral;
                if (header.Length + 4 + bodyStr.Length > 79)
                    return header + Environment.NewLine + Parsing.Formatting.Indent("=> " + bodyStr);
                else
                    return header + " => " + bodyStr;

            }
        }
    }
}
