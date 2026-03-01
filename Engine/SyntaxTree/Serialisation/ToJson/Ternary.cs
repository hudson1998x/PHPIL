using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class TernaryNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");

        builder.Append(",\"condition\": ");
        if (Condition is not null)
            Condition.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append(",\"then\": ");
        if (Then is not null)
            Then.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append(",\"else\": ");
        if (Else is not null)
            Else.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append('}');
    }
}