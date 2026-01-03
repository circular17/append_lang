using System.Diagnostics.CodeAnalysis;

namespace Append.Memory
{
    /// <summary>
    /// Stocke un handle vers un objet sur le tas. 
    /// </summary>
    public readonly struct Handle(long value) : IEquatable<Handle>
    {
        public readonly long Value = value;
        public readonly bool Equals(Handle other) => Value == other.Value;
        public override readonly string ToString() => $"Handle({Value})";
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
            =>  obj is Handle h && Equals(h);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(Handle left, Handle right)
            => left.Value == right.Value;
        public static bool operator !=(Handle left, Handle right)
            =>  left.Value != right.Value;
    }
}
