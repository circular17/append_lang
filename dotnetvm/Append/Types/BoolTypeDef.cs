using Append.Memory;

namespace Append.Types
{
    public class BoolTypeDef()
        : TypeDef("bool", [])
    {
        public static readonly string[] BoolLitterals = ["no", "yes"];

        public override Value MakeValueFromLiteral(TypeId typeId, string text, Heap heap)
        {
            bool value;
            if (text == BoolLitterals[0])
                value = false;
            else if (text == BoolLitterals[1])
                value = true;
            else
                throw new Exception("Bool litteral expected (" + string.Join(", ", BoolLitterals) + ")");

            if (TypeManager.IsRefType(typeId))
                return Value.FromHandle(heap.AddObject(value), typeId);
            else
            {
                if (typeId != TypeId.Bool)
                    throw new Exception("Incorrect type id");
                else
                    return Value.FromBool(value);
            }
        }

        public static bool GetValue(Value value, Heap heap)
        {
            if (value.IsReference)
                return (bool)heap.GetObject(value.Data.Handle);
            else
                return value.Data.Bool;
        }

        public override string ToLiteral(Value value, Heap heap)
        {
            return GetValue(value, heap) ? BoolLitterals[1] : BoolLitterals[0];
        }
    }
}

