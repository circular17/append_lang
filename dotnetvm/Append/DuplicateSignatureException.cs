namespace Append
{
    internal class DuplicateSignatureException : Exception
    {
        public DuplicateSignatureException() : base("Duplicate signature for function.") { }
    }
}
