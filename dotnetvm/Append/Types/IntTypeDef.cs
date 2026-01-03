using Append.Memory;
using System.Globalization;

namespace Append.Types
{
    public class IntTypeDef()
        : TypeDef("int", [])
    {
        public override Value MakeValueFromLiteral(TypeId typeId, string text, Heap heap)
        {
            if (long.TryParse(text, CultureInfo.InvariantCulture, out var value))
            {
                if (TypeManager.IsRefType(typeId))
                    return Value.FromHandle(heap.AddObject(value), typeId);
                else
                {
                    if (typeId != TypeId.Int)
                        throw new Exception("Incorrect type id");
                    else
                        return Value.FromInt(value);
                }
                    
            }
            else
                throw new Exception($"Integer litteral expected ({long.MinValue}..{long.MaxValue})");
        }

        public static long GetValue(Value value, Heap heap)
        {
            if (value.IsReference)
                return (long)heap.GetObject(value.Data.Handle);
            else
                return value.Data.Int;
        }

        public override string ToLiteral(Value value, Heap heap)
        {
            return GetValue(value, heap).ToString(CultureInfo.InvariantCulture);
        }
    }
}
