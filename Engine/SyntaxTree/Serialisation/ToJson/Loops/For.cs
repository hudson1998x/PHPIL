using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Partial implementation of the <see cref="For"/> node providing JSON serialization.
/// </summary>
public partial class For
{
    /// <summary>
    /// Serializes this <see cref="For"/> loop node to JSON format.
    /// </summary>
    /// <param name="span">The source code span.</param>
    /// <param name="tokens">The token stream.</param>
    /// <param name="builder">The builder to append JSON text to.</param>
    public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
    {
        builder.Append('{');
        builder.Append($"\"type\": \"{GetType().Name}\"");

        // 1. Init ($i = 0)
        builder.Append(",\"init\": ");
        if (Init is not null)
            Init.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        // 2. Condition ($i < 10)
        builder.Append(",\"condition\": ");
        if (Condition is not null)
            Condition.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        // 3. Increment ($i++)
        builder.Append(",\"increment\": ");
        if (Increment is not null)
            Increment.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        // 4. Body { ... }
        builder.Append(",\"body\": ");
        if (Body is not null)
            Body.ToJson(span, tokens, builder);
        else
            builder.Append("null");

        builder.Append('}');
    }
}