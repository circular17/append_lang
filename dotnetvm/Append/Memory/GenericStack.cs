namespace Append.Memory
{
    public class GenericStack
    {
        private readonly List<Value> _stack = [];

        public int Count => _stack.Count;

        public void Push(Value value)
            => _stack.Add(value);

        public Value Pop()
        {
            if (_stack.Count == 0)
                throw new Exception("Stack is empty");
            var index = _stack.Count - 1;
            var result = _stack[index];
            _stack.RemoveAt(index);
            return result;
        }

        public Value Peek(int index)
            => _stack[_stack.Count - 1 - index];

        public Value Poke(int index, Value value)
            => _stack[_stack.Count - 1 - index] = value;

        public void EnterFrame(GenericStack source, int frameSize)
        {
            for (int i = 0; i < frameSize; i++)
                Push(source.Pop());
        }

        public void ExitFrame(int frameSize)
        {
            _stack.RemoveRange(_stack.Count - frameSize, frameSize);
        }

        public void Clear()
        {
            _stack.Clear();
        }
    }
}
