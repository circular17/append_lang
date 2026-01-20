using Append.AST;
using Append.Types;

namespace Append
{
    internal class VariableResolver(Scope GlobalScope)
    {
        internal HashSet<ASTNode> VisitedNodes { get; } = [];

        public void ResolveVariables(ASTVarContainer varContainer, TypeManager typeManager)
        {
            VisitedNodes.Clear();
            InternalResolveVariables(varContainer, GlobalScope, typeManager, varContainer);
        }

        private void InternalResolveVariables(ASTNode root, Scope parentScope, TypeManager typeManager, ASTVarContainer varContainer)
        {
            if (VisitedNodes.Contains(root))
                return;
            VisitedNodes.Add(root);

            if (root is ASTFunction f)
            {
                if (f.Scope == null)
                {
                    f.Scope = new Scope("func " + f.Name, parentScope);
                    for (int i = 0; i < f.ParameterCount; i++)
                    {
                        var parameter = f.GetParameter(i);
                        if (parameter.TypeId == TypeId.None)
                        {
                            if (parentScope.TryFindType(parameter.TypeName, out var found))
                            {
                                // prefer value type
                                parameter.TypeId =
                                    found.ValueTypeId != TypeId.None ?
                                    found.ValueTypeId : found.RefTypeId;
                            }
                        }
                        f.Scope.AddVariable(parameter);
                    }
                }
                else if (f.Scope.Parent != parentScope)
                    throw new UnexpectedParentScopeException();
                if (f.Body != null)
                    InternalResolveVariables(f.Body, f.Scope, typeManager, f);
                return;
            }
            else
            {
                if (root is ASTDefineVar def)
                {
                    def.Variable ??= varContainer.AddLocalVariable(def.Name, def.TypeName);
                    if (def.Variable.TypeId == TypeId.None)
                    {
                        if (parentScope.TryFindType(def.TypeName, out var found))
                        {
                            // prefer value type
                            def.Variable.TypeId =
                                found.ValueTypeId != TypeId.None ?
                                found.ValueTypeId : found.RefTypeId;
                        }
                    }
                    parentScope.AddVariable(def.Variable);
                }
                else if (root is ASTWriteVar write)
                {
                    if (parentScope.TryFindVariable(write.VarName, out var variable))
                        write.Variable = variable;
                    else
                        throw new VariableNotFoundException();
                }
                else if (root is ASTReadVar read)
                {
                    if (parentScope.TryFindVariable(read.VarName, out var variable))
                        read.Variable = variable;
                    else
                        throw new VariableNotFoundException();
                }
                for (int i = 0; i < root.SubNodeCount; i++)
                    InternalResolveVariables(root.GetSubNode(i), parentScope, typeManager, varContainer);
            }    
        }
    }
}
