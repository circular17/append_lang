using Append.Memory;

namespace Append.Types
{
    public class StringTypeDef()
        : TypeDef("string", [])
    {
        public override Value MakeValueFromLiteral(TypeId type, string text, Heap heap)
        {
            if (!TypeManager.IsRefType(type))
                throw new Exception("A string is always a reference");            

            if (text.StartsWith('\"') && text.EndsWith('\"'))
            {
                string s = text[1..^1].Replace("\"\"", "\"").
                    Replace("{{", "{").Replace("}}", "}");
                return Value.FromHandle(heap.AddObject(s), type);
            }
            else
                throw new Exception($"String litteral expected (\"...\")");
        }

        public static string GetValue(Value value, Heap heap)
        {
            if (value.IsReference)
                return (string)heap.GetObject(value.Data.Handle);
            else
                throw new Exception("Reference type expected");
        }

        public override string ToLiteral(Value value, Heap heap)
        {
            return $"\"{GetValue(value, heap).Replace("\"", "\"\"")
                .Replace("{", "{{").Replace("}", "}}")}\"";
        }
    }
}
