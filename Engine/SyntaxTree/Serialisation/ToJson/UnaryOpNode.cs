using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class UnaryOpNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");
        builder.Append($",\"operator\": \"{Operator}\"");
        builder.Append($",\"prefix\": {(Prefix ? "true" : "false")}");

        builder.Append(",\"operand\": ");
        if (Operand is not null)
            Operand.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append('}');
    }
}