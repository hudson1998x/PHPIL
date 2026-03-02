using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers
{
    /// <summary>
    /// Parses and produces a complete PHP <c>while</c> statement:
    /// <code>
    /// while (expr) { ... }
    /// </code>
    /// 
    /// <para>
    /// This producer creates a <see cref="WhileNode"/> with its <see cref="WhileNode.Expression"/>
    /// and <see cref="WhileNode.Body"/> populated based on the parsed tokens.
    /// </para>
    /// 
    /// <para>
    /// Inherits from <see cref="KeywordExpressionBlock"/>, which handles the core mechanics
    /// of parsing a keyword followed by a parenthesized expression and a block body.
    /// </para>
    /// </summary>
    public class WhileExpression : KeywordExpressionBlock
    {
        /// <summary>
        /// Shadows the base <see cref="KeywordExpressionBlock.Node"/> with a strongly-typed
        /// <see cref="WhileNode"/> property.
        /// </summary>
        /// <remarks>
        /// The <c>new</c> keyword is required because <see cref="KeywordExpressionBlock.Node"/>
        /// returns a generic <see cref="SyntaxNode"/>.
        /// </remarks>
        public new WhileNode? Node { get; private set; }

        /// <summary>
        /// Creates a new <see cref="WhileNode"/> instance from the given range, expression, and body.
        /// Called by <see cref="KeywordExpressionBlock.Init"/> after successfully matching a while statement.
        /// </summary>
        /// <param name="start">The start index of the matched token range.</param>
        /// <param name="end">The end index of the matched token range.</param>
        /// <param name="expr">The conditional expression node inside the while parentheses.</param>
        /// <param name="body">The block node representing the body of the while loop.</param>
        /// <returns>A <see cref="WhileNode"/> representing the parsed while statement.</returns>
        public override SyntaxNode CreateNode(int start, int end, ExpressionNode? expr, BlockNode? body)
        {
            return new WhileNode
            {
                RangeStart = start,
                RangeEnd   = end,
                Expression = expr,
                Body       = body
            };
        }

        /// <summary>
        /// Initializes the <see cref="WhileExpression"/> producer.
        /// Builds the base <c>while(expr){body}</c> parser and wraps it to produce
        /// a fully typed <see cref="WhileNode"/> in <see cref="Node"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="Producer"/> delegate that parses tokens, producing a match result
        /// and populating the <see cref="Node"/> property.
        /// </returns>
        public override Producer Init()
        {
            // Build the base while(expr){body} producer once.
            var baseProducer = base.Init();

            return (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
            {
                // Parse the mandatory `while (expr) { body }` core.
                var baseMatch = baseProducer(tokens, source, pointer);
                if (!baseMatch.Success)
                    return baseMatch;

                // Rebuild the final WhileNode from the base result.
                Node = new WhileNode
                {
                    RangeStart = baseMatch.Start,
                    RangeEnd   = baseMatch.End,
                    Expression = (base.Node as WhileNode)?.Expression,
                    Body       = (base.Node as WhileNode)?.Body
                };

                return new Match(true, baseMatch.Start, baseMatch.End);
            };
        }
    }
}