using Append.AST;

namespace Append
{
    public class FunctionResolver(Scope GlobalScope)
    {
        internal List<(ASTNode? Parent, ASTCall Call)> UnresolvedCalls { get; } = [];
        internal List<(ASTNode? Parent, ASTCall Call)> ResolvedCalls { get; } = [];
        internal HashSet<ASTNode> VisitedNodes { get; } = [];

        private readonly Stack<ASTFunction> _functionStack = [];
        private readonly HashSet<ASTFunction> _addedFunctions = [];

        public void ResolveFunctions(ASTMain root, bool verbose = false)
        {
            _functionStack.Clear();
            _addedFunctions.Clear();
            var doing = true;
            while (doing)
            {
                int resolvedCount = ResolvedCalls.Count;
                VisitedNodes.Clear();
                UnresolvedCalls.Clear();
                _ = InternalResolveFunctions(null, GlobalScope, root);
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

        public ASTNode InternalResolveFunctions(ASTNode? parent, Scope scope, ASTNode node)
        {
            if (VisitedNodes.Contains(node))
                return node;

            VisitedNodes.Add(node);

            bool inFunction = false;

            if (node is ASTFunction g && !_addedFunctions.Contains(g))
            {
                scope.AddFunction(g);
                _addedFunctions.Add(g);
            }

            if (parent is ASTFunction f)
            {
                _functionStack.Push(f);
                inFunction = true;
                if (f.Scope != null)
                    scope = f.Scope;
            }

            for (int i = 0; i < node.SubNodeCount; i++)
            {
                var prevNode = node.GetSubNode(i);
                var newNode = InternalResolveFunctions(node, scope, prevNode);
                if (newNode != prevNode)
                    node.SetSubNode(i, newNode);
            }
            if (node is ASTCall call)
            {
                if (scope.TryFindFunction(call.FunctionName, call.KnownParameterTypes, out var function))
                {
                    ResolvedCalls.Add((parent, call));
                    bool isReturning = node.IsReturning;

                    if (function.IsIntrinsic && function.ParameterCount == 2 
                        && function.Body is ASTBinaryIntrinsic binary)
                        node = new ASTBinaryIntrinsic(call.Parameters[0], call.Parameters[1], binary.Operation);
                    else
                    {
                        if (_functionStack.Count > 0 && _functionStack.Peek() == function && node.IsReturning)
                            node = new ASTResolvedTailCall(function, call.Parameters);
                        else
                            node = new ASTResolvedCall(function, call.Parameters);
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
