using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        var escaped = Token.TextValue(span).Replace("\\", "\\\\").Replace("\"", "\\\"");

        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");
        builder.Append($",\"name\": \"{escaped}\"");
        builder.Append($",\"evaluatedType\": \"{AnalysedType}\"");
        builder.Append('}');
    }
}