
namespace Append.AST
{
    public class ASTBreak : ASTNodeWithoutSubNodes
    {
        internal override Types.TypeId KnownType => Types.TypeId.None;

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            return (ASTSignal.Break, null);
        }

        internal override string ToString(int surroundingPriority)
            => "break";
    }
}
