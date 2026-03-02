using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches the common pattern of a keyword followed by a parenthesised expression
/// and a brace-delimited block body — the shape shared by <c>while</c>, <c>for</c>,
/// and <c>foreach</c> in PHP.
///
/// <para>
/// Rather than duplicating this structure three times, the parser dispatches all
/// three loop keywords here and subclasses override <see cref="CreateNode"/> to
/// produce the correct <see cref="SyntaxNode"/> type for each. The keyword token
/// itself is consumed by <c>AnyToken()</c> — by the time this production is
/// reached, the dispatcher in <c>Parser.TryProduce</c> has already confirmed which
/// keyword we're looking at, so we don't need to re-validate it here.
/// </para>
/// </summary>
public class KeywordExpressionBlock : Production
{
    /// <summary>
    /// The AST node produced on a successful match. The concrete type depends on
    /// which subclass is in use — <c>null</c> until <see cref="Init"/> fires and
    /// <see cref="CreateNode"/> is called by the <c>Capture</c> callback.
    /// </summary>
    public virtual SyntaxNode? Node { get; private set; }

    /// <summary>
    /// Factory method called on a successful match to construct the appropriate
    /// <see cref="SyntaxNode"/> for the specific keyword this production represents.
    ///
    /// <para>
    /// The base implementation returns a plain <see cref="SyntaxNode"/> and is
    /// intended to be overridden by concrete subclasses (<c>WhileExpression</c>,
    /// <c>ForExpression</c>, <c>ForeachExpression</c>, etc.) that produce their own
    /// typed nodes. Keeping construction here rather than in the <c>Capture</c>
    /// lambda directly means subclasses only need to override this one method —
    /// the entire combinator pipeline is inherited as-is.
    /// </para>
    /// </summary>
    /// <param name="start">Token index of the opening keyword.</param>
    /// <param name="end">Token index just past the closing brace of the body block.</param>
    /// <param name="expr">The parsed condition or iterator expression, if any.</param>
    /// <param name="body">The parsed body block, if any.</param>
    public virtual SyntaxNode CreateNode(int start, int end, ExpressionNode? expr, BlockNode? body)
    {
        return new SyntaxNode();
    }

    public override Producer Init()
    {
        // Instantiate sub-productions up front so their Node properties are
        // accessible inside the Capture callback below. If they were created
        // inside the returned delegate they'd be local to each invocation and
        // the references captured by the lambda would be stale.
        var expr  = new Expression();
        var block = new Block();

        // Reusable helper for skipping optional formatting between the keyword,
        // expression, and block — lets the rule accept both compact and spaced styles.
        var whitespaceOrNewline = AnyOf(
            Prefab<Whitespace>(),
            Prefab<NewLine>()
        );

        return Capture(
            Sequence(
                AnyToken(),                    // Consume the keyword (while/for/foreach).
                                               // The dispatcher already validated the kind,
                                               // so we just need to advance past it.
                Optional(whitespaceOrNewline),
                expr.Init(),                   // Match the condition/iterator expression and
                                               // populate expr.Node for use in CreateNode.
                Optional(whitespaceOrNewline),
                block.Init()                   // Match the brace-delimited body and
                                               // populate block.Node for use in CreateNode.
            ),
            (start, end) =>
            {
                // The entire sequence matched — hand off to CreateNode so the correct
                // typed node is built. Subclasses receive both sub-nodes already
                // populated, so they never need to touch the token stream directly.
                Node = CreateNode(start, end, expr?.Node, block?.Node);
            }
        );
    }
}