using Append.Types;

namespace Append.AST
{
    public abstract class ASTVarContainer : ASTNode
    {
        public Scope? Scope { get; set; }

        public abstract Variable AddLocalVariable(string name, string typeName);
    }
}
