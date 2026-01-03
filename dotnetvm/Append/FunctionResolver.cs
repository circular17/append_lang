using Append.AST;

namespace Append
{
    public class FunctionResolver(Scope Scope)
    {
        internal List<(ASTNode? Parent, ASTCallByName Call)> UnresolvedCalls { get; } = [];
        internal List<(ASTNode? Parent, ASTCallByName Call)> ResolvedCalls { get; } = [];
        internal HashSet<ASTNode> VisitedNodes { get; } = [];

        private readonly Stack<ASTFunction> _functionStack = [];

        public void ResolveFunctions(ref ASTNode root, bool verbose = false)
        {
            _functionStack.Clear();
            var doing = true;
            while (doing)
            {
                int resolvedCount = ResolvedCalls.Count;
                VisitedNodes.Clear();
                UnresolvedCalls.Clear();
                root = InternalResolveFunctions(null, root);
                doing = resolvedCount < ResolvedCalls.Count;
            }

            if (verbose && ResolvedCalls.Count != 0)
            {
                Console.WriteLine("Resolved calls");
                foreach (var (Parent, Call) in ResolvedCalls)
                {
                    if (Parent != null)
                        Console.WriteLine(Call.ToString() + " in " + Parent.GetType().Name);
                    else
                        Console.WriteLine(Call.ToString() + " as root");
                }
            }
            if (UnresolvedCalls.Count != 0)
            {
                Console.WriteLine("Unresolved calls");
                foreach (var (Parent, Call) in UnresolvedCalls)
                {
                    if (Parent != null)
                        Console.WriteLine(Call.ToString() + " in " + Parent.GetType().Name);
                    else
                        Console.WriteLine(Call.ToString() + " as root");
                }
            }
        }

        public ASTNode InternalResolveFunctions(ASTNode? parent, ASTNode node)
        {
            if (VisitedNodes.Contains(node))
                return node;

            VisitedNodes.Add(node);

            bool inFunction = false;
            if (parent is ASTFunction f)
            {
                _functionStack.Push(f);
                inFunction = true;
            }

            node.ReplaceSubNodes(InternalResolveFunctions);
            if (node is ASTCallByName call)
            {
                if (Scope.TryFindFunction(call.FunctionName, call.KnownParameterTypes, out var function))
                {
                    ResolvedCalls.Add((parent, call));
                    bool isReturning = node.IsReturning;

                    if (function.IsIntrinsic && function.ParameterCount == 2 
                        && function.Body is ASTBinaryIntrinsic binary)
                        node = new ASTBinaryIntrinsic(call.Parameters[0], call.Parameters[1], binary.Operation);
                    else
                    {
                        if (_functionStack.Count > 0 && _functionStack.Peek() == function && node.IsReturning)
                            node = new ASTTailCall(function, call.Parameters);
                        else
                            node = new ASTCall(function, call.Parameters);
                    }

                    node.IsReturning = isReturning;
                }
                else
                    UnresolvedCalls.Add((parent, call));
            }

            if (inFunction)
                _functionStack.Pop();
            return node;
        }
    }
}
