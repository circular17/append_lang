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
        {
            if (index < 0)
                return _stack[^(-index)];
            else
                return _stack[index];
        }

        public void Poke(int index, Value value)
        {
            if (index < 0)
              _stack[^(-index)] = value;
            else
              _stack[index] = value;
        }

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
