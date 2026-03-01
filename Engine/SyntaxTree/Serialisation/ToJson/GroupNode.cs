using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class GroupNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");

        builder.Append(",\"inner\": ");
        if (Inner is not null)
            Inner.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append('}');
    }
}