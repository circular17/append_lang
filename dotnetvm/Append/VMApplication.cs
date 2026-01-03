using Append.AST;

namespace Append
{
    public class VMApplication
    {
        protected readonly Scope _globalScope = new("Global", null);
        protected readonly Types.TypeManager _types = new();
        protected readonly Memory.Heap _heap = new();

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

        public Value RunProgram(ASTNode program, bool verbose = true)
        {
            var thread = InitThread();
            var resolver = new FunctionResolver(_globalScope);
            resolver.ResolveFunctions(ref program);
            if (verbose)
            {
                _globalScope.PrintFunctions();
                Console.WriteLine(program.ToString());
            }
            thread.Run(program, false);
            if (verbose)
            {
                Console.WriteLine("==> " + thread.Value);
                Console.WriteLine();
            }
            return thread.Value;
        }
    }
}
