using Append.Memory;
using System.Globalization;

namespace Append.Types
{
    public class FloatTypeDef()
        : TypeDef("float", [])
    {
        public override Value MakeValueFromLiteral(TypeId typeId, string text, Heap heap)
        {
            if (double.TryParse(text, CultureInfo.InvariantCulture, out var value))
            {
                if (TypeManager.IsRefType(typeId))
                    return Value.FromHandle(heap.AddObject(value), typeId);
                else
                {
                    if (typeId != TypeId.Float)
                        throw new Exception("Incorrect type id");
                    else
                        return Value.FromFloat(value);
                }
            }
            else
                throw new Exception($"Float litteral expected ({double.MinValue}..{double.MaxValue})");
        }

        public static double GetValue(Value value, Heap heap)
        {
            if (value.IsReference)
                return (double)heap.GetObject(value.Data.Handle);
            else
                return value.Data.Float;
        }

        public override string ToLiteral(Value value, Heap heap)
        {
            return GetValue(value, heap).ToString(CultureInfo.InvariantCulture);
        }
    }
}
