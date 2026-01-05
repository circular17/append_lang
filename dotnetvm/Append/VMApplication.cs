using Append.AST;

namespace Append
{
    public class VMApplication
    {
        protected readonly Scope _globalScope = new("Global", null);
        protected readonly Types.TypeManager _types = new();
        protected readonly Memory.Heap _heap = new();
        protected ASTNode? _main;

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
            _main = main;
        }

        public string ProgramToString()
        {
            var content = new List<string>();
            var functions = _globalScope.FunctionsToString();
            if (!string.IsNullOrWhiteSpace(functions))
                content.Add(functions);

            if (_main != null)
                content.Add(_main.ToString());

            return string.Join(Environment.NewLine, content);
        }

        public Value Run(bool verbose = true)
        {
            if (_main == null)
            {
                if (verbose)
                    Console.WriteLine("No main.");
                return Value.Void;
            }
            var thread = InitThread();
            var resolver = new FunctionResolver(_globalScope);
            resolver.ResolveFunctions(ref _main);
            if (verbose)
                Console.WriteLine(ProgramToString());
            thread.Run(_main, false);
            if (verbose)
            {
                Console.WriteLine("==> " + thread.Value);
                Console.WriteLine();
            }
            return thread.Value;
        }
    }
}
