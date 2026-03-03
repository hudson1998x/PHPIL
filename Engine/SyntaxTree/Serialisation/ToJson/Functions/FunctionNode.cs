using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class FunctionNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");
        builder.Append($",\"name\": \"{Name.TextValue(span)}\"");

        builder.Append(",\"params\": [");
        var needsComma = false;
        foreach (var param in Params)
        {
            if (needsComma) builder.Append(',');
            builder.Append('{');
            if (param.TypeHint.HasValue)
            {
                builder.Append($"\"typeHint\": \"{param.TypeHint.Value.TextValue(span)}\"");
            }   
            builder.Append($",\"name\": \"{param.Name.TextValue(span)}\"");
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