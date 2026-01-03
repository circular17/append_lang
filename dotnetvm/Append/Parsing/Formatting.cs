namespace Append.Parsing
{
    internal class Formatting
    {
        public static string Indent(string text, string indent = "  ")
        {
            return string.Join(Environment.NewLine,
                from s in text.Split(Environment.NewLine)
                select indent + s);
        }
    }
}
