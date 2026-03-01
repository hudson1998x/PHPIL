using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class FunctionCallNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");

        builder.Append(",\"callee\": ");
        if (Callee is not null)
            Callee.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append(",\"args\": [");
        var needsComma = false;
        foreach (var arg in Args)
        {
            if (needsComma) builder.Append(',');
            arg.ToJson(span, tokens, builder);
            needsComma = true;
        }
        builder.Append(']');

        builder.Append('}');
    }
}