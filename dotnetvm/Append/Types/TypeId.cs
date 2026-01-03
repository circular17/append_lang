namespace Append.Types
{
    public enum TypeId : long
    {
        None = 0,
        Void = 1,
        Bool = 2,
        Int = 3,
        Float = 4,

        RefTypeFlag = unchecked((long)0x8000000000000000),

        BoxedBool = RefTypeFlag + 1,
        BoxedInt = RefTypeFlag + 2,
        BoxedFloat = RefTypeFlag + 3,

        String = RefTypeFlag + 4
    }
}
