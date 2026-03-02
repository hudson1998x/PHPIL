using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches an <c>else</c> clause of the form <c>else { ... }</c> and produces
/// a <see cref="BlockNode"/> representing the else body.
///
/// <para>
/// Unlike <see cref="ElseIfExpression"/>, which produces its own dedicated node
/// type, <c>else</c> carries no condition — its entire semantic content is the
/// body block. Exposing <see cref="Node"/> as a <see cref="BlockNode"/> directly
/// (rather than wrapping it in an <c>ElseNode</c>) reflects this: the parent
/// <see cref="IfExpression"/> stores it in its <c>ElseNode</c> property and can
/// use it without unwrapping an intermediate container.
/// </para>
/// </summary>
public class ElseExpression : Production
{
    /// <summary>
    /// The body block of the <c>else</c> clause.
    /// <c>null</c> until <see cref="Init"/> fires successfully.
    /// </summary>
    public BlockNode? Node { get; private set; }

    public override Producer Init()
    {
        // Allocated outside the delegate for the same closure-stability reason as
        // ElseIfExpression — the Capture callback reads block.Node after Init() runs,
        // so it must reference the same instance that the Sequence executed against.
        var block = new Block();
        var whitespaceOrNewline = AnyOf(Prefab<Whitespace>(), Prefab<NewLine>());

        return Capture(
            Sequence(
                Token(TokenKind.Else),         // Must open with `else` — no condition follows.
                Optional(whitespaceOrNewline),
                block.Init()                   // The entire semantic content of this clause.
            ),
            (start, end) =>
            {
                // A successful Sequence guarantees block.Init() matched, so block.Node
                // should never be null here. The explicit guard exists as a defensive
                // assertion: if the Block production ever changes in a way that allows
                // it to succeed without populating its Node, this surfaces the breakage
                // immediately with a clear message rather than propagating a silent null
                // into the IfNode and failing somewhere downstream.
                if (block.Node is null)
                {
                    throw new InvalidOperationException("Expected a block, received null");
                }

                // Unwrap the block directly — no intermediate wrapper node needed since
                // `else` has no condition or other fields of its own.
                Node = block.Node;

                // Patch the range to cover the full clause (`else { ... }`) rather than
                // just the block's braces. The Block production's own range starts at `{`,
                // but the parent IfNode's range should account for the `else` keyword too.
                Node.RangeStart = start;
                Node.RangeEnd   = end;
            });
    }
}