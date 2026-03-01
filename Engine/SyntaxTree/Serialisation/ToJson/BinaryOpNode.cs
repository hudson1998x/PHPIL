using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class BinaryOpNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");
        builder.Append($",\"operator\": \"{Operator}\"");

        builder.Append(",\"left\": ");
        if (Left is not null)
            Left.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append(",\"right\": ");
        if (Right is not null)
            Right.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append('}');
    }
}