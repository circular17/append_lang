namespace Append.Parsing
{
    internal class Operations
    {
        public const string Indexing = "[ ]";   
        public const string Mutation = "mut";
        public const string Exponentiation = "^";
        public const string Composition = ">>";
        public const string BitNegationStrong = "!";
        public const string FunctionCall = "( )";
        public const string Multiplication = "* / %";
        public const string Addition = "+ -";
        public const string Range = "..";
        public const string Concat = "++";
        public const string Link = "::";
        public const string Coalescing = "??";
        public const string Comparison = "< <= = != >= >";
        public const string BitAnd = "&";
        public const string BitXor = "xor";
        public const string BitOr = "or";
        public const string BitNegationWeak = "not";
        public const string Ternary = "? else";
        public const string Lambda = "=>";
        public const string Piping = "\\";
        public const string Case = "|";

        public static bool IsOperator(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (name[0] < '0' || name[0] > '9' && name[0] < 'A'
                || name[0] > 'Z' && name[0] < 'a'
                || name[0] > 'z' && name[0] <= '~')
                return true;

            return name == BitXor || name == BitOr || name == BitNegationWeak;

        }

        public static string[] AllOperations =
        {
            Indexing,
            Mutation,
            Exponentiation,
            Composition,
            BitNegationStrong,
            FunctionCall,
            Multiplication,
            Addition,
            Range,
            Concat,
            Link,
            Coalescing,
            Comparison,
            BitAnd,
            BitXor,
            BitOr,
            BitNegationWeak,
            Ternary,
            Lambda,
            Piping,
            Case
        };

        public static int Priorty(string operation)
        {
            var operationSpace = operation + " ";
            var spaceOperation = " " + operation;
            var spaceOperationSpace = " " + operationSpace;
            for (int i = 0; i < AllOperations.Length; i++)
            {
                if (AllOperations[i] == operation || AllOperations[i].StartsWith(operationSpace)
                    || AllOperations[i].EndsWith(spaceOperation) || AllOperations[i].Contains(spaceOperationSpace))
                    return AllOperations.Length - i;
            }
            return 0;
        }

        internal static string Brackets(string span, string operation, int surroundingPriority)
        {
            return Brackets(span, Priorty(operation), surroundingPriority);
        }
        internal static string Brackets(string span, int priority, int surroundingPriority)
        {
            if (priority > surroundingPriority)
                return span;
            else
                return $"({span})";
        }
    }
}
