using System.Text;

namespace PHPIL.Engine.CodeLexer;

/// <summary>
/// Basic structure for a token identified by the Lexer. 
/// </summary>
public struct Token
{
    /// <summary>
    /// Where does this token start?
    /// </summary>
    public int RangeStart;
    
    /// <summary>
    /// Where does this token end?
    /// </summary>
    public int RangeEnd;

    // Compatibility aliases for existing code that expects Start/Length
    public int Start
    {
        get => RangeStart;
        set => RangeStart = value;
    }
    public int Length => RangeEnd - RangeStart;
    
    /// <summary>
    /// What type of token is this?
    /// </summary>
    public TokenKind Kind;

    public void ToJson(in ReadOnlySpan<char> span, StringBuilder builder)
    {
        builder.Append('{');

        var raw = span.Slice(RangeStart, RangeEnd - RangeStart).ToString();
        var escaped = raw
            .Replace("\\", "\\\\")  // must be first to avoid double-escaping
            .Replace("\"", "\\\"")  // quote
            .Replace("\n", "\\n")   // newline
            .Replace("\r", "\\r")   // carriage return
            .Replace("\t", "\\t")   // tab
            .Replace("\b", "\\b")   // backspace
            .Replace("\f", "\\f");  // form feed

        builder.Append($"\"value\": \"{escaped}\",");
        builder.Append($"\"kind\": \"{Kind}\",");
        builder.Append($"\"start\": {RangeStart},");
        builder.Append($"\"end\": {RangeEnd},");
        builder.Append($"\"length\": {RangeEnd - RangeStart}");
        
        builder.Append('}');
    }

    public string TextValue(in ReadOnlySpan<char> span)
    {
        return span.Slice(RangeStart, RangeEnd - RangeStart).ToString();
    }
}
