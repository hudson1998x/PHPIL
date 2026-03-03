using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class IdentifierNode
{
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");
        builder.Append($",\"kind\": \"{Token.Kind}\"");
        builder.Append($",\"token\": ");
        Token.ToJson(in span, builder);
        builder.Append('}');
    }
}