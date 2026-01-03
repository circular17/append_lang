using Append.Memory;
using Append.Types;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Append
{
    public struct Value(TypeId typeId, Data64 data)
    {
        public readonly TypeId TypeId = typeId;
        public Data64 Data = data;

        public static Value Void => new(TypeId.Void, new Data64());

        public static Value FromBool(bool value)
            => new(TypeId.Bool, new Data64 { Bool = value });

        public static Value FromInt(long value)
            => new(TypeId.Int, new Data64 { Int = value });

        public static Value FromFloat(double value)
            => new(TypeId.Float, new Data64 { Float = value });

        public static Value FromHandle(Handle Handle, TypeId Type)
            => new(Type, new Data64 { Handle = Handle });

        public readonly bool IsReference => TypeManager.IsRefType(TypeId);

        public override string ToString()
        {
            return TypeId switch
            {
                TypeId.Void => VoidTypeDef.VoidLitteral,
                TypeId.Bool => BoolTypeDef.BoolLitterals[Data.Bool ? 1 : 0],
                TypeId.Int => Data.Int.ToString(CultureInfo.InvariantCulture),
                TypeId.Float => Data.Float.ToString(CultureInfo.InvariantCulture),
                _ => IsReference ? "<ref>" : "<const>",
            };
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Data64
    {
        [FieldOffset(0)] public bool Bool;
        [FieldOffset(0)] public long Int;
        [FieldOffset(0)] public double Float;
        [FieldOffset(0)] public Handle Handle;
    }
}
