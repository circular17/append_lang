using Append.Types;

namespace Append.AST
{
    public record BinaryIntrinsic(string Name, TypeId LeftType, TypeId RightType,
        Func<Value, Value, Value> Apply, TypeId ResultType)
    {
        public static readonly BinaryIntrinsic[] BinaryIntrinsics =
            [
                new BinaryIntrinsic("+", TypeId.Int, TypeId.Int, (l, r) => Value.FromInt(l.Data.Int + r.Data.Int), TypeId.Int),
                new BinaryIntrinsic("-", TypeId.Int, TypeId.Int, (l, r) => Value.FromInt(l.Data.Int - r.Data.Int), TypeId.Int),
                new BinaryIntrinsic("*", TypeId.Int, TypeId.Int, (l, r) => Value.FromInt(l.Data.Int * r.Data.Int), TypeId.Int),
                new BinaryIntrinsic("/", TypeId.Int, TypeId.Int, (l, r) => Value.FromInt(l.Data.Int / r.Data.Int), TypeId.Int),
                new BinaryIntrinsic("%", TypeId.Int, TypeId.Int, (l, r) => Value.FromInt(l.Data.Int % r.Data.Int), TypeId.Int),
                
                new BinaryIntrinsic("<", TypeId.Int, TypeId.Int, (l, r) => Value.FromBool(l.Data.Int < r.Data.Int), TypeId.Bool),
                new BinaryIntrinsic("<=", TypeId.Int, TypeId.Int, (l, r) => Value.FromBool(l.Data.Int <= r.Data.Int), TypeId.Bool),
                new BinaryIntrinsic("=", TypeId.Int, TypeId.Int, (l, r) => Value.FromBool(l.Data.Int == r.Data.Int), TypeId.Bool),
                new BinaryIntrinsic("!=", TypeId.Int, TypeId.Int, (l, r) => Value.FromBool(l.Data.Int != r.Data.Int), TypeId.Bool),
                new BinaryIntrinsic(">=", TypeId.Int, TypeId.Int, (l, r) => Value.FromBool(l.Data.Int >= r.Data.Int), TypeId.Bool),
                new BinaryIntrinsic(">", TypeId.Int, TypeId.Int, (l, r) => Value.FromBool(l.Data.Int > r.Data.Int), TypeId.Bool),

                new BinaryIntrinsic("+", TypeId.Float, TypeId.Float, (l, r) => Value.FromFloat(l.Data.Float + r.Data.Float), TypeId.Float),
                new BinaryIntrinsic("-", TypeId.Float, TypeId.Float, (l, r) => Value.FromFloat(l.Data.Float - r.Data.Float), TypeId.Float),
                new BinaryIntrinsic("*", TypeId.Float, TypeId.Float, (l, r) => Value.FromFloat(l.Data.Float * r.Data.Float), TypeId.Float),
                new BinaryIntrinsic("/", TypeId.Float, TypeId.Float, (l, r) => Value.FromFloat(l.Data.Float / r.Data.Float), TypeId.Float),
                new BinaryIntrinsic("%", TypeId.Float, TypeId.Float, (l, r) => Value.FromFloat(l.Data.Float % r.Data.Float), TypeId.Float),

                new BinaryIntrinsic("<", TypeId.Float, TypeId.Float, (l, r) => Value.FromBool(l.Data.Float < r.Data.Float), TypeId.Bool),
                new BinaryIntrinsic("<=", TypeId.Float, TypeId.Float, (l, r) => Value.FromBool(l.Data.Float <= r.Data.Float), TypeId.Bool),
                new BinaryIntrinsic("=", TypeId.Float, TypeId.Float, (l, r) => Value.FromBool(l.Data.Float == r.Data.Float), TypeId.Bool),
                new BinaryIntrinsic("!=", TypeId.Float, TypeId.Float, (l, r) => Value.FromBool(l.Data.Float != r.Data.Float), TypeId.Bool),
                new BinaryIntrinsic(">=", TypeId.Float, TypeId.Float, (l, r) => Value.FromBool(l.Data.Float >= r.Data.Float), TypeId.Bool),
                new BinaryIntrinsic(">", TypeId.Float, TypeId.Float, (l, r) => Value.FromBool(l.Data.Float > r.Data.Float), TypeId.Bool),            ];
    }
}
