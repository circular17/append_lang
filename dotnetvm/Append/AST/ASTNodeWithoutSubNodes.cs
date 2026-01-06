namespace Append.AST
{
    public abstract class ASTNodeWithoutSubNodes : ASTNode
    {
        internal override int SubNodeCount => 0;
        internal override ASTNode GetSubNode(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
