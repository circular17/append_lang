namespace Append.Memory
{
    public class Heap
    {
        const int MaxObjectCount = int.MaxValue;

        private readonly List<(int Generation, object? Obj)> _objects = [];
        private readonly Stack<int> _freeSlots = new();

        public Handle AddObject(object obj)
        {
            if (_freeSlots.TryPop(out int index))
            {
                int generation = _objects[index].Generation + 1;
                _objects[index] = (generation, obj);
                return MakeHandle(generation, index);
            }
            else 
            {
                if (_objects.Count >= MaxObjectCount)
                    throw new Exception("No more handles");

                var newIndex = _objects.Count;
                var generation = 0;
                _objects.Add((generation, obj));
                return MakeHandle(generation, newIndex);
            }
        }

        public object GetObject(Handle handle)
        {
            var (generation, index) = DeconstructHandle(handle);
            if (index < 0 || index > _objects.Count)
                throw new Exception("Handle out of bounds");

            var (curGeneration, curObj) = _objects[index];
            if (generation > curGeneration)
                throw new Exception("Anachronic generation");

            if (generation == curGeneration && curObj != null)
                return curObj;
            else
                throw new Exception("Object was removed");
        }

        public bool Remove(Handle handle)
        {
            var (generation, index) = DeconstructHandle(handle);
            if (index < 0 || index > _objects.Count)
                throw new Exception("Handle out of bounds");

            var (curGeneration, curObj) = _objects[index];
            if (generation > curGeneration)
                throw new Exception("Anachronic generation");

            if (curGeneration == generation && curObj != null)
            {
                _objects[index] = (curGeneration, null);
                return true;
            }
            else
                return false; // already removed
        }

        private static (int Generation, int Index) DeconstructHandle(Handle handle)
        {
            return ((int)(handle.Value >> 32), (int)(handle.Value & 0x7FFFFFFF));
        }

        private static Handle MakeHandle(int generation, int index)
            => new((long)index | (long)generation << 32);
    }
}
