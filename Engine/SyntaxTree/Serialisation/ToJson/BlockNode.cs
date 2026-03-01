using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class BlockNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");
        builder.Append(",\"statements\": [");

        var needsComma = false;
        foreach (var statement in Statements)
        {
            if (needsComma) builder.Append(',');
            statement.ToJson(span, tokens, builder);
            needsComma = true;
        }

        builder.Append(']');
        builder.Append('}');
    }
}