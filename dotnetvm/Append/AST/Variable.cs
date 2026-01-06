namespace Append.AST
{
    public record Variable(string Name, Types.TypeId TypeId, Types.TypeManager TypeManager)
    {
        public int StackIndex { get; set; }

        public override string ToString()
            => TypeId == Types.TypeId.None ? Name : $"{Name}: {TypeManager.GetTypeDef(TypeId).Name}";
    }
}
