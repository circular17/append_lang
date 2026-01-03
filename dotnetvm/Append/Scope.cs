using System.Diagnostics.CodeAnalysis;

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

        public bool TryFindType(string name, out (Types.TypeId ValueTypeId, Types.TypeId RefTypeId) type)
        {
            if (_types.TryGetValue(name, out var dualType))
            {
                type = dualType.ToTuple();
                return true;
            }
            else
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
            function = null;
            return false;
        }

        internal void Clear()
        {
            _functions.Clear();
            _types.Clear();
        }

        internal void PrintFunctions()
        {
            foreach (var list in _functions.Values)
            {
                foreach (var f in list)
                    if (!f.IsIntrinsic)
                    {
                        Console.WriteLine(f.ToString());
                        Console.WriteLine();
                    }
            }
        }
    }
}
