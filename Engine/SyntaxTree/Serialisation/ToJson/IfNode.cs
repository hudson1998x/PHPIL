using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class IfNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");

        builder.Append(",\"expression\": ");
        if (Expression is not null)
            Expression.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append(",\"body\": ");
        if (Body is not null)
            Body.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append(",\"elseIfs\": [");
        var needsComma = false;
        foreach (var elseIf in ElseIfs)
        {
            if (needsComma) builder.Append(',');
            elseIf.ToJson(span, tokens, builder);
            needsComma = true;
        }
        builder.Append(']');

        builder.Append(",\"else\": ");
        if (ElseNode is not null)
            ElseNode.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append('}');
    }
}