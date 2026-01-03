using Append.Memory;

namespace Append.Types
{
    public abstract class TypeDef
    {
        public readonly string Name;
        public readonly TypeId[] Parameters;

        public TypeDef(string name, TypeId[] parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        public abstract Value MakeValueFromLiteral(TypeId typeId, string text, Heap heap);

        public abstract string ToLiteral(Value value, Heap heap);
    }
}