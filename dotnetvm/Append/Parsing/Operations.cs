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
        public const string BitXorOr = "xor or";
        public const string BitNegationWeak = "not";
        public const string Ternary = "? else";
        public const string Lambda = "\\";
        public const string Case = "|";
        public const string Piping = "->";
        public const string Assignment = ":= += -= *= /= %= ^= &= ++=";

        public static bool IsOperatorChar(char c)
        {
            return c < '0' || c > '9' && c < 'A'
                || c > 'Z' && c < 'a'
                || c > 'z' && c <= '~';
        }

        public static bool IsOperator(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (IsOperatorChar(name[0]))
                return true;

            return name == "xor" || name == "or" || name == BitNegationWeak
                || name == Mutation || name == "else";
        }

        public static string[] AllOperations =
        [
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
            BitXorOr,
            BitNegationWeak,
            Ternary,
            Lambda,
            Case,
            Piping,
            Assignment,
        ];

        private static readonly int _functionCallPriority = 
            AllOperations.Length - Array.IndexOf(AllOperations, FunctionCall);

        public static int Priorty(string operation)
        {
            if (IsOperator(operation))
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
            else
                return _functionCallPriority;
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
