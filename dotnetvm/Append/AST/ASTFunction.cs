namespace Append.AST
{
    public class ASTFunction : ASTNode
    {
        public string Name { get; }
        private readonly List<ASTLocalVar> _parameters = [];
        private readonly List<ASTLocalVar> _locals = [];
        private readonly Dictionary<string, ASTLocalVar> _varByName = [];

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

        public ASTFunction(string name, Types.TypeManager typeManager,
            (string Name, Types.TypeId Type)[]? parameters = null, 
            (string Name, Types.TypeId Type)[]? locals = null)
        {
            Name = name;
            var reverseStackIndex = 0;
            if (locals != null)
                foreach (var local in locals.Reverse())
                {
                    if (_varByName.ContainsKey(local.Name))
                        throw new Exceptions.DuplicateVariableNameException();
                    var astVar = new ASTLocalVar(local.Name, local.Type, typeManager, reverseStackIndex);
                    _locals.Add(astVar);
                    reverseStackIndex++;
                    _varByName.Add(local.Name, astVar);
                }

            if (parameters != null)
                foreach (var param in parameters)
                {
                    if (_varByName.ContainsKey(param.Name))
                        throw new Exceptions.DuplicateVariableNameException();
                    var astVar = new ASTLocalVar(param.Name, param.Type, typeManager, reverseStackIndex);
                    _parameters.Add(astVar);
                    reverseStackIndex++;
                    _varByName.Add(param.Name, astVar);
                }
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
        public Types.TypeId[] KnownParameterTypes =>
            [.. from p in _parameters select p.TypeId];

        private Types.TypeId _knownType = Types.TypeId.None;
        internal override Types.TypeId KnownType
        {
            get
            {
                if (_knownType == Types.TypeId.None && Body != null)
                {
                    _knownType = Body.ReturnType;
                    if (_knownType == Types.TypeId.None)
                    {
                        _knownType = Body.KnownType;
                    }
                }
                return _knownType;
            }
        } 

        public ASTLocalVar? FindVariable(string name)
        {
            _varByName.TryGetValue(name, out var result);
            return result;
        }

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            if (Body != null) 
                Body = replaceFunction(this, Body);
        }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            if (Body == oldNode)
                Body = newNode;
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

        internal bool SameParameterTypes(Types.TypeId[] parameterTypeIds)
        {
            if (parameterTypeIds.Length != ParameterCount)
                return false;
            for (int i = 0; i < ParameterCount; i++)
            {
                if (parameterTypeIds[i] != _parameters[i].TypeId
                    || _parameters[i].TypeId == Types.TypeId.None)
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
                header = $"fun () '{Name}'";
            else if (_parameters.Count == 1)
                header = $"fun {_parameters[0]} '{Name}'";
            else
                header = $"fun {_parameters[0]} '{Name}' {string.Join("; ", _parameters.Skip(1))}";

            var locals = _locals.Count == 0 ? ""
                : Parsing.Formatting.Indent(string.Join(Environment.NewLine, _locals.Select(l => "var " + l.ToString()))) + Environment.NewLine;
            if (_locals.Count > 0 || Body is ASTBlock)
            {
                return header + " {" + Environment.NewLine + locals +
                    Parsing.Formatting.Indent(Body?.ToString() ?? Types.VoidTypeDef.VoidLitteral) +
                    Environment.NewLine + "}";
            }
            else
            {
                var bodyStr = Body?.ToString() ?? Types.VoidTypeDef.VoidLitteral;
                if (header.Length + 4 + bodyStr.Length > 79)
                    return header + Environment.NewLine + Parsing.Formatting.Indent("=> " + bodyStr);
                else
                    return header + " => " + bodyStr;

            }
        }
    }
}
