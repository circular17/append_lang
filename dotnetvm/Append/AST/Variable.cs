using Append.Types;
using System;

namespace Append.AST
{
    public class Variable(string name, string typeName)
    {
        public string Name { get; } = name;
        public string TypeName { get; } = typeName;

        public TypeId TypeId { get; set; } = TypeId.None;

        public int StackIndex { get; set; }

        public override string ToString()
            => TypeName == "" ? Name : $"{Name}: {TypeName}";
    }
}
