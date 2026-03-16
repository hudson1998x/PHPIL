namespace PHPIL.Engine.Runtime.Sdk;

public static partial class SdkInitializer
{
    internal static readonly MemoryStream StdoutMemory = new();
    internal static readonly StreamWriter StdoutStream = new(StdoutMemory);

    static void InitStreams()
    {
        Sdk.Function("print")
            .Takes<object>()
            .Calls(Streams.Print);
        
        Sdk.Function("print_r")
            .Takes<object>()
            .Calls(Streams.PrintR);
        
        Sdk.Function("var_dump")
            .Takes<object>()
            .Calls(Streams.VarDump);
    }
}

public static partial class Streams
{
    public static void Print(object value)
    {
        string str = value switch
        {
            bool b => b ? "1" : "",
            null => "",
            _ => value.ToString()?.Replace("\\n", "\n") ?? ""
        };
        SdkInitializer.StdoutStream.Write(str);
    }

    public static object PrintR(object value)
    {
        string output = FormatValue(value, 0);
        SdkInitializer.StdoutStream.Write(output);
        return true;
    }

    public static void VarDump(object value)
    {
        string output = FormatVarDump(value, 0);
        SdkInitializer.StdoutStream.Write(output);
    }

    private static string FormatValue(object value, int depth)
    {
        string indent = new string(' ', depth * 4);
        string nextIndent = new string(' ', (depth + 1) * 4);

        return value switch
        {
            null => "NULL",
            bool b => b ? "true" : "false",
            int i => i.ToString(),
            float f => f.ToString(),
            double d => d.ToString(),
            string s => s,
            Dictionary<object, object> dict => FormatArray(dict, depth, indent, nextIndent),
            _ => value.ToString() ?? ""
        };
    }

    private static string FormatArray(Dictionary<object, object> dict, int depth, string indent, string nextIndent)
    {
        if (dict.Count == 0)
            return "Array\n(\n)";

        var lines = new List<string> { "Array" };
        lines.Add("(");

        foreach (var kvp in dict)
        {
            lines.Add($"{nextIndent}[{kvp.Key}] => {FormatValue(kvp.Value, depth + 1)}");
        }

        lines.Add($"{indent})");
        return string.Join("\n", lines);
    }

    private static string FormatVarDump(object value, int depth)
    {
        string indent = new string(' ', depth * 4);
        string nextIndent = new string(' ', (depth + 1) * 4);

        return value switch
        {
            null => "NULL",
            bool b => $"bool({(b ? "true" : "false")})",
            int i => $"int({i})",
            float f => $"float({f})",
            double d => $"double({d})",
            string s => $"string({s.Length}) \"{s}\"",
            Dictionary<object, object> dict => FormatVarDumpArray(dict, depth, indent, nextIndent),
            _ => $"{value.GetType().Name}({value})"
        };
    }

    private static string FormatVarDumpArray(Dictionary<object, object> dict, int depth, string indent, string nextIndent)
    {
        if (dict.Count == 0)
            return "array(0) {\n}";

        var lines = new List<string> { $"array({dict.Count}) {{" };

        foreach (var kvp in dict)
        {
            lines.Add($"{nextIndent}[{kvp.Key}]=>\n{nextIndent}{FormatVarDump(kvp.Value, depth + 1)}");
        }

        lines.Add($"{indent}}}");
        return string.Join("\n", lines);
    }
}