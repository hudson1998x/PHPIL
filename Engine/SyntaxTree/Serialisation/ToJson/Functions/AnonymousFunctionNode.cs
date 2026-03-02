using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class AnonymousFunctionNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");

        builder.Append(",\"params\": [");
        var needsComma = false;
        foreach (var param in Params)
        {
            if (needsComma) builder.Append(',');
            builder.Append('{');
            builder.Append($"\"typeHint\": \"{param.TypeHint.TextValue(span)}\"");
            builder.Append($",\"name\": \"{param.Name.TextValue(span)}\"");
            builder.Append('}');
            needsComma = true;
        }
        builder.Append(']');

        builder.Append(",\"use\": [");
        needsComma = false;
        foreach (var capture in UseCaptures)
        {
            if (needsComma) builder.Append(',');
            builder.Append('{');
            builder.Append($"\"name\": \"{capture.Name.TextValue(span)}\"");
            builder.Append($",\"byRef\": {(capture.ByRef ? "true" : "false")}");
            builder.Append('}');
            needsComma = true;
        }
        builder.Append(']');

        builder.Append(",\"body\": ");
        if (Body is not null)
            Body.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append('}');
    }
}