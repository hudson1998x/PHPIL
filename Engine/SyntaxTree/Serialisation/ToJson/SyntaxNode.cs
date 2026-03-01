using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class SyntaxNode
{
    public virtual void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");
        builder.Append('}');
    }
}