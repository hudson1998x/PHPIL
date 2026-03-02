using System.Text;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree
{
    /// <summary>
    /// Represents a PHP <c>while</c> statement in the syntax tree.
    /// Provides serialization of the node and its children into JSON.
    /// </summary>
    public partial class WhileNode
    {
        /// <summary>
        /// Serializes this <see cref="WhileNode"/> to JSON format, writing
        /// the result into the provided <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="span">
        /// The source code span from which the tokens were derived.
        /// Used by child nodes to reference source text if needed.
        /// </param>
        /// <param name="tokens">
        /// The token stream corresponding to the source code. Child nodes may
        /// use this to serialize token-specific information.
        /// </param>
        /// <param name="builder">
        /// The <see cref="StringBuilder"/> instance into which the JSON output
        /// is written. The method appends directly to this builder.
        /// </param>
        /// <remarks>
        /// <para>
        /// Produces a JSON object with the following structure:
        /// <code>
        /// {
        ///   "type": "WhileNode",
        ///   "expression": { ... } | null,
        ///   "body": { ... } | null
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// The <c>expression</c> property represents the loop condition.
        /// The <c>body</c> property represents the loop body block.
        /// If either is <c>null</c>, the JSON explicitly sets the value to <c>null</c>.
        /// </para>
        /// <para>
        /// This method recursively calls <c>ToJson</c> on child nodes to produce
        /// nested JSON structures.
        /// </para>
        /// </remarks>
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

            builder.Append('}');
        }
    }
}