using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Append
{
    internal class DualType
    {
        public Types.TypeId ValueTypeId { get; set; }
        public Types.TypeId RefTypeId { get; set; }

        public (Types.TypeId ValueTypeId, Types.TypeId RefTypeId) ToTuple()
            => (ValueTypeId, RefTypeId);
    }

    public record Scope(string Name, Scope? Parent)
    {

        private readonly Dictionary<string, DualType> _types = [];

        private readonly Dictionary<string, List<AST.ASTFunction>> _functions = [];

        public IEnumerable<AST.ASTFunction> AllFunctions
            => _functions.Values.SelectMany(list => list);

        private readonly Dictionary<string, AST.Variable> _variables = [];

        public void AddType(Types.TypeId typeId, Types.TypeDef typeDef)
        {
            if (!_types.TryGetValue(typeDef.Name, out var dualType))
            {
                dualType = new DualType();
                _types.Add(typeDef.Name, dualType);
            }
            if (Types.TypeManager.IsRefType(typeId))
            {
                if (dualType.RefTypeId != Types.TypeId.None)
                    throw new Exception("Type already defined in scope");
                dualType.RefTypeId = typeId;
            }
            else
            {
                if (dualType.ValueTypeId != Types.TypeId.None)
                    throw new Exception("Type already defined in scope");
                dualType.ValueTypeId = typeId;
            }
        }

        public void AddFunction(AST.ASTFunction function)
        {
            if (!_functions.TryGetValue(function.Name, out var list))
            {
                list = [];
                _functions.Add(function.Name, list);
            }
            if (list.Any(f => f.SameParameterTypes(function)))
                throw new DuplicateSignatureException();
            list.Add(function);
        }

        public void AddVariable(AST.Variable Variable)
        {
            _variables.Add(Variable.Name, Variable);
        }

        public bool TryFindType(string name, out (Types.TypeId ValueTypeId, Types.TypeId RefTypeId) type)
        {
            if (_types.TryGetValue(name, out var dualType))
            {
                type = dualType.ToTuple();
                return true;
            }
            else if (Parent != null)
                return Parent.TryFindType(name, out type);
            {
                type = (Types.TypeId.None, Types.TypeId.None);
                return false;
            }
        }

        public bool TryFindFunction(string functionName, Types.TypeId[] knownParameterTypes,
            [NotNullWhen(true)] out AST.ASTFunction? function)
        {
            if (_functions.TryGetValue(functionName, out var list))
            {
                foreach (var f in list)
                {
                    if (f.SameParameterTypes(knownParameterTypes))
                    {
                        function = f;
                        return true;
                    }
                }
            }
            if (Parent != null)
                return Parent.TryFindFunction(functionName, knownParameterTypes, out function);
            function = null;
            return false;
        }

        public bool TryFindVariable(string variableName, [NotNullWhen(true)]out AST.Variable? variable)
        {
            if (!_variables.TryGetValue(variableName, out variable))
            {
                if (Parent != null)
                    return Parent.TryFindVariable(variableName, out variable);
                else
                    return false;
            }
            else
                return true;
        }

        internal void Clear()
        {
            _functions.Clear();
            _types.Clear();
        }

        internal string FunctionsToString()
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var list in _functions.Values)
            {
                foreach (var f in list)
                    if (!f.IsIntrinsic)
                    {
                        if (first)
                            first = false;
                        else
                            sb.AppendLine();

                        sb.AppendLine(f.ToString());
                    }
            }
            return sb.ToString();
        }
    }
}
