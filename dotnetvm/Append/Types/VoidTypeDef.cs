using Append.Memory;

namespace Append.Types
{
    public class VoidTypeDef()
        : TypeDef("void", [])
    {
        public const string VoidLitteral = "()";

        public override Value MakeValueFromLiteral(TypeId typeId, string text, Heap heap)
        {
            if (TypeManager.IsRefType(typeId))
                throw new Exception("Void cannot be referenced");

            if (text != VoidLitteral)
                throw new Exception("Void litteral expected (" + VoidLitteral + ")");

            return Value.Void;
        }

        public override string ToLiteral(Value value, Heap heap)
        {
            return VoidLitteral;
        }
    }
}
