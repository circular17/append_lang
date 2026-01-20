
using Append.AST;

namespace Append.Types
{
    internal class NativeTypes
    {
        public static void AddNativeTypes(TypeManager typeManager, Scope scope)
        {
            var voidType = new VoidTypeDef();
            var boolType = new BoolTypeDef();
            var intType = new IntTypeDef();
            var floatType = new FloatTypeDef();
            var stringType = new StringTypeDef();

            RegisterType(typeManager, scope, TypeId.Void, voidType);
            RegisterType(typeManager, scope, TypeId.Bool, boolType);
            RegisterType(typeManager, scope, TypeId.Int, intType);
            RegisterType(typeManager, scope, TypeId.Float, floatType);

            RegisterType(typeManager, scope, TypeId.BoxedBool, boolType);
            RegisterType(typeManager, scope, TypeId.BoxedInt, intType);
            RegisterType(typeManager, scope, TypeId.BoxedFloat, floatType);
            RegisterType(typeManager, scope, TypeId.String, stringType);

            foreach (var intrinsic in BinaryIntrinsic.BinaryIntrinsics)
            {
                scope.AddFunction(new ASTFunction(intrinsic.Name, 
                    parameters: [("a", typeManager.GetTypeDef(intrinsic.LeftType).Name), 
                        ("b", typeManager.GetTypeDef(intrinsic.RightType).Name)])
                {
                    Body = new ASTBinaryIntrinsic(new ASTReadVar("a"),
                        new ASTReadVar("b"), intrinsic),
                    IsIntrinsic = true
                });
            }
        }

        private static void RegisterType(TypeManager typeManager, Scope scope, TypeId typeId, TypeDef typeDef)
        {
            if (TypeManager.IsRefType(typeId))
                typeManager.AddRefType(typeId, typeDef);
            else
                typeManager.AddValueType(typeId, typeDef);

            scope.AddType(typeId, typeDef);
        }
    }
}
