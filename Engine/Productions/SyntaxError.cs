namespace PHPIL.Engine.Productions;

public class SyntaxError : Exception
{
    public int Line { get; }
    public int Column { get; }
    public string Token { get; }
    public string Expected { get; }
    public string Context { get; }

    public SyntaxError(string message, int line, int column, string token, string expected, string context)
        : base($"{message} at line {line}, column {column}: found '{token}', expected {expected}")
    {
        Line = line;
        Column = column;
        Token = token;
        Expected = expected;
        Context = context;
    }

    public override string ToString()
    {
        return $"Syntax Error at line {Line}, column {Column}:\n" +
               $"Found: '{Token}'\n" +
               $"Expected: {Expected}\n" +
               $"Context:\n{Context}";
    }
}
