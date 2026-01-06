using Append.AST;

namespace Append
{
    public class VMApplication
    {
        protected readonly Scope _globalScope = new("Global", null);
        protected readonly Types.TypeManager _types = new();
        protected readonly Memory.Heap _heap = new();
        protected ASTMain? _main;

        public VMApplication()
        { 
            Reset();
        }

        protected void Reset()
        {
            _globalScope.Clear();
            _types.Clear();
            Types.NativeTypes.AddNativeTypes(_types, _globalScope);
        }

        protected VMThread InitThread()
        {
            return new VMThread(_types, _heap);
        }

        protected void SetMain(ASTNode main)
        {
            if (main is ASTMain container)
                _main = container;
            else
                _main = new ASTMain([main]);
        }

        public string ProgramToString()
        {
            return _main?.ToString() ?? "";
        }

        public Value CompileAndRun(bool verbose = false)
        {
            if (_main == null)
            {
                if (verbose)
                    Console.WriteLine("No main.");
                return Value.Void;
            }
            var varResolver = new VariableResolver(_globalScope);
            varResolver.ResolveVariables(_main, _types);
            foreach (var f in _globalScope.AllFunctions)
                varResolver.ResolveVariables(f, _types);
            var funcResolver = new FunctionResolver(_globalScope);
            funcResolver.ResolveFunctions(_main);
            if (verbose)
                Console.WriteLine(ProgramToString());
            return Run(verbose);
        }

        private Value Run(bool verbose = false)
        {
            if (_main == null)
            {
                if (verbose)
                    Console.WriteLine("No main.");
                return Value.Void;
            }
            var thread = InitThread();
            thread.Run(_main, verbose);
            if (verbose)
            {
                Console.WriteLine("==> " + thread.Value);
                Console.WriteLine();
            }
            return thread.Value;
        }
    }
}
