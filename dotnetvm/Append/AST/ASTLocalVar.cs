namespace Append.AST
{
    public record ASTLocalVar(string Name, Types.TypeId TypeId, Types.TypeManager TypeManager, int ReverseStackIndex)
    {
        public override string ToString()
            => TypeId == Types.TypeId.None ? Name : $"{Name} {TypeManager.GetTypeDef(TypeId).Name}";
    }
}
