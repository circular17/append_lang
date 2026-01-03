namespace Append
{
    /// <summary>
    /// Signal returned by AST nodes
    /// </summary>
    internal enum ASTSignal
    {
        /// <summary>
        /// Stay on current node
        /// </summary>
        Continue,
        /// <summary>
        /// Push a new node to run
        /// </summary>
        Enter,
        /// <summary>
        /// Performs a tail call (parameters in the evaluation stack)
        /// </summary>
        TailCall,
        /// <summary>
        /// Jump to a new node to run (not storing current node)
        /// </summary>
        Jump,
        /// <summary>
        /// Node is done (result in current value)
        /// </summary>
        Done,

        /// <summary>
        /// Return from current function (control)
        /// </summary>
        Return,
        /// <summary>
        /// Exit the current loop (control)
        /// </summary>
        Break,
        /// <summary>
        /// Go to next element in the loop (control)
        /// </summary>
        Next
    }

    /// <summary>
    /// Gestion de 
    /// </summary>
    internal struct UnwindHandler
    {
        public AST.ASTNode Creator;
        public int FrameDepth;
        public int NodeDepth;
        public int EvalDepth;
        public int ReturnStep;
        public int BreakStep;
        public int NextStep;

        public readonly int GetStep(ASTSignal signal)
        {
            return signal switch
            {
                ASTSignal.Return => ReturnStep,
                ASTSignal.Break => BreakStep,
                ASTSignal.Next => NextStep,
                _ => throw new NotSupportedException()
            };
        }
    }

    /// <summary>
    /// Thread context with stacks and current instruction
    /// </summary>
    public class VMThread(Types.TypeManager Types, Memory.Heap Heap)
    {
        /// <summary>
        /// Stack for storing frames (parameters et local variables)
        /// </summary>
        private readonly Memory.GenericStack _frameStack = new();

        /// <summary>
        /// Allocates space on the frame stack, initializing with values from evaluation stack
        /// </summary>
        /// <param name="frameSize"></param>
        internal void EnterFrame(int frameSize)
        {
            _frameStack.EnterFrame(_evaluationStack, frameSize);
        }

        /// <summary>
        /// Deallocates space on the frame (<paramref name="frameSize"/> need to be the same as when calling EnterFrame)
        /// </summary>
        internal void ExitFrame(int frameSize)
        {
            _frameStack.ExitFrame(frameSize);
        }

        /// <summary>
        /// Reads a local variable or a parameter (index starting from last value)
        /// </summary>
        internal Value PeekFrame(int index)
        {
            return _frameStack.Peek(index);
        }

        /// <summary>
        /// Sets a local variable or parameter (index starting from last value)
        /// </summary>
        internal void PokeFrame(int index, Value value)
        {
            _frameStack.Poke(index, value);
        }

        private readonly List<UnwindHandler> _unwindStack = [];

        internal void PushUnwind(AST.ASTNode creator, int returnStep = -1, int breakStep = -1, int nextStep = -1)
        {
            _unwindStack.Add(new UnwindHandler
            {
                Creator = creator,
                FrameDepth = _frameStack.Count,
                EvalDepth = _evaluationStack.Count,
                NodeDepth = _nodeStack.Count,
                ReturnStep = returnStep,
                BreakStep = breakStep,
                NextStep = nextStep
            });
        }
        
        internal void PopUnwind(AST.ASTNode creator)
        {
            if (_unwindStack.Count > 0 && _unwindStack[^1].Creator == creator)
                _unwindStack.RemoveAt(_unwindStack.Count - 1);
            else
                throw new InvalidOperationException();
        }

        /// <summary>
        /// Prepare a tail call
        /// </summary>
        /// <param name="parameterCount">Parameters to be copied from evaluation stack</param>
        private void PrepareTailCall(int parameterCount)
        {
            for (int i = _unwindStack.Count - 1; i >= 0; --i)
            {
                var handler = _unwindStack[i];
                if (handler.ReturnStep != -1)
                {
                    while (_frameStack.Count > handler.FrameDepth)
                        _frameStack.Pop();
                    // update parameters
                    for (int j = parameterCount - 1; j >= 0; j--)
                        _frameStack.Poke(j, _evaluationStack.Pop());

                    while (_nodeStack.Count > handler.NodeDepth + 1)
                        _nodeStack.Pop();
                    while (_evaluationStack.Count > handler.EvalDepth)
                        _evaluationStack.Pop();

                    _unwindStack.RemoveRange(i + 1, _unwindStack.Count - (i + 1));
                    return;
                }
            }
            throw new InvalidOperationException();
        }

        private void UnwindTo(ASTSignal kind)
        {
            for (int i = _unwindStack.Count - 1; i >= 0; --i)
            {
                var handler = _unwindStack[i];
                var restoreStep = handler.GetStep(kind);
                if (restoreStep != -1)
                {
                    while (_frameStack.Count > handler.FrameDepth)
                        _frameStack.Pop();
                    while (_nodeStack.Count > handler.NodeDepth + 1)
                        _nodeStack.Pop();
                    while (_evaluationStack.Count > handler.EvalDepth)
                        _evaluationStack.Pop();
                    _unwindStack.RemoveRange(i + 1, _unwindStack.Count - (i + 1));
                    (_currentNode, _) = _nodeStack.Pop();
                    _nodeStep = restoreStep;
                    return;
                }
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Stack for node navigation
        /// </summary>
        private readonly Stack<(AST.ASTNode Node, int Step)> _nodeStack = new();

        /// <summary>
        /// Current node
        /// </summary>
        private AST.ASTNode? _currentNode;

        /// <summary>
        /// Step in current node
        /// </summary>
        private int _nodeStep;

        /// <summary>
        /// Stack for storing values in expressions
        /// </summary>
        private readonly Memory.GenericStack _evaluationStack = new();

        /// <summary>
        /// Current value of expression
        /// </summary>
        public Value Value = Value.Void;

        /// <summary>
        /// Stores current value to stack
        /// </summary>
        internal void PushValue() => _evaluationStack.Push(Value);

        /// <summary>
        /// Retrieves value from stack
        /// </summary>
        internal void PopValue() => Value = _evaluationStack.Pop();

        /// <summary>
        /// Setup execution with given AST node
        /// </summary>
        public void Start(AST.ASTNode node)
        {
            _currentNode = node;
            _nodeStep = 0;
            _frameStack.Clear();
            _nodeStack.Clear();
            _evaluationStack.Clear();
            Value = Value.Void;
        }

        /// <summary>
        /// One step of execution through AST nodes
        /// </summary>
        public bool Step()
        {
            if (_currentNode != null)
            {
                var (signal, nextNode) = _currentNode.Step(this, ref _nodeStep);
                switch (signal)
                {
                    case ASTSignal.Continue:
                        return true;

                    case ASTSignal.Done:
                        if (_nodeStack.Count > 0)
                        {
                            (_currentNode, _nodeStep) = _nodeStack.Pop();
                            return true;
                        }
                        else
                        {
                            _currentNode = null;
                            _nodeStep = 0;
                            return false;
                        }

                    case ASTSignal.Enter:
                        _nodeStack.Push((_currentNode, _nodeStep));
                        _currentNode = nextNode;
                        _nodeStep = 0;
                        return true;

                    case ASTSignal.TailCall:
                        var function = (AST.ASTFunction?)nextNode;
                        if (function == null)
                            throw new AST.Exceptions.MissingNodeException();
                        PrepareTailCall(function.ParameterCount);
                        _currentNode = function;
                        _nodeStep = 0;
                        return true;

                    case ASTSignal.Jump:
                        _currentNode = nextNode;
                        _nodeStep = 0;
                        return true;

                    default:
                        UnwindTo(signal);
                        return true;
                }
            }
            else
                return false;
        }

        public void Run(AST.ASTNode program, bool verbose = false)
        {
            Start(program);
            while (true)
            {
                bool stepResult;
                if (verbose)
                {
                    var debugInfo = GetStateDebugInfo();
                    stepResult = Step();
                    Console.WriteLine($"{debugInfo} -> value {DebugValue(Value)}");
                }
                else
                    stepResult = Step();

                if (!stepResult)
                    break;
            }
        }

        public string DebugValue(Value value)
        {
            if (value.TypeId == Append.Types.TypeId.None)
                return "undefined";
            var typeDef = Types.GetTypeDef(value.TypeId);
            return typeDef.ToLiteral(value, Heap);
        }

        public string GetStateDebugInfo()
        {
            return $"[frames={_frameStack.Count}, unwind={_unwindStack.Count}, nodes={_nodeStack.Count}, evals={_evaluationStack.Count}] {_currentNode?.GetType()?.Name} at {_nodeStep}";
        }

    }
}
