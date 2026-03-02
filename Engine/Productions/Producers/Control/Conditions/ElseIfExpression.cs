using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches a single <c>elseif</c> clause of the form <c>elseif (expr) { ... }</c>
/// and produces an <see cref="ElseIfNode"/> capturing the condition and body.
///
/// <para>
/// Intentionally models only one branch — the <c>elseif</c> chain is assembled by
/// <see cref="IfExpression"/>, which loops over fresh instances of this class until
/// no further <c>elseif</c> token is found. Keeping this production single-branch
/// means it stays simple and composable, and the loop in <see cref="IfExpression"/>
/// remains the single place responsible for chain length.
/// </para>
///
/// <para>
/// Unlike <see cref="IfExpression"/>, this one is clean enough to express purely
/// as a combinator chain — the structure is fixed (keyword, expression, block) with
/// no variable-length sub-lists or multi-part conditional logic, so <c>Capture</c>
/// over a <c>Sequence</c> is all that's needed.
/// </para>
/// </summary>
public class ElseIfExpression : Production
{
    /// <summary>
    /// The AST node produced on a successful match.
    /// <c>null</c> until <see cref="Init"/> fires successfully.
    /// </summary>
    public ElseIfNode? Node { get; private set; }

    public override Producer Init()
    {
        // Allocate sub-productions outside the returned delegate so their Node
        // properties are stable references that the Capture callback can read.
        // Creating them inside the delegate would produce new instances on each
        // invocation, and the lambda would close over the wrong (outer) references.
        var expr  = new Expression();
        var block = new Block();

        // Shared trivia skipper — allows any whitespace or newlines between the
        // elseif keyword, the condition expression, and the opening brace.
        var whitespaceOrNewline = AnyOf(Prefab<Whitespace>(), Prefab<NewLine>());

        return Capture(
            Sequence(
                Token(TokenKind.ElseIf),       // Must open with the `elseif` keyword —
                                               // distinguishes this from a bare `else`.
                Optional(whitespaceOrNewline),
                expr.Init(),                   // Condition expression (typically parenthesised,
                                               // though the Expression production handles that).
                Optional(whitespaceOrNewline),
                block.Init()                   // Body block — delegated to Block so nested
                                               // statements work without any extra effort here.
            ),
            (start, end) => Node = new ElseIfNode
            {
                RangeStart = start,
                RangeEnd   = end,
                Expression = expr.Node,        // Populated by expr.Init() on success.
                Body       = block.Node        // Populated by block.Init() on success.
            }
        );
    }
}