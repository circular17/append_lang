namespace Append.Types
{
    public class TypeManager
    {
        private readonly List<TypeDef> _valueTypes = [];
        private readonly List<TypeDef> _refTypes = [];

        public void AddValueType(TypeId typeId, TypeDef typeDef)
        {
            if (IsRefType(typeId))
                throw new Exception("Unexpected reference type");

            if (GetTypeIndex(typeId) != _valueTypes.Count)
                throw new Exception("Type id doesn't match index");

            _ = AddValueType(typeDef);
        }

        public TypeId AddValueType(TypeDef typeDef)
        {
            var index = _valueTypes.Count;
            _valueTypes.Add(typeDef);
            var typeId = (TypeId)(index + 1);
            return typeId;
        }

        public void AddRefType(TypeId typeId, TypeDef typeDef)
        {
            if (!IsRefType(typeId))
                throw new Exception("Unexpected value type");

            if (GetTypeIndex(typeId) != _refTypes.Count)
                throw new Exception("Type id doesn't match index");

            _ = AddRefType(typeDef);
        }

        public TypeId AddRefType(TypeDef typeDef)
        {
            var index = _refTypes.Count;
            _refTypes.Add(typeDef);
            var typeId = (TypeId)(index + 1) | TypeId.RefTypeFlag;
            return typeId;
        }

        public static bool IsRefType(TypeId typeId)
            => (typeId & TypeId.RefTypeFlag) != 0;

        public static int GetTypeIndex(TypeId typeId)
            => (int)(typeId & ~TypeId.RefTypeFlag) - 1;

        public TypeDef GetTypeDef(TypeId typeId)
        {
            if (IsRefType(typeId))
                return _refTypes[GetTypeIndex(typeId)];
            else
                return _valueTypes[GetTypeIndex(typeId)];
        }

        internal void Clear()
        {
            _refTypes.Clear();
            _valueTypes.Clear();
        }
    }
}
