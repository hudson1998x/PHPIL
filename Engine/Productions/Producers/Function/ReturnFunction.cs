using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;
using System;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches a <c>return</c> statement, with or without a return value.
/// Produces a <see cref="ReturnNode"/> whose <c>Expression</c> is <c>null</c>
/// for bare <c>return;</c> and populated for <c>return &lt;expr&gt;;</c>.
///
/// <para>
/// Like <see cref="VariableAssignment"/>, this is written imperatively rather
/// than as a combinator chain. The conditional expression — present for value
/// returns, absent for bare returns — is easier to handle with a straightforward
/// <c>if</c> than with <c>Optional</c> and a <c>Capture</c> callback, since we
/// need to conditionally assign to a local before building the node.
/// </para>
/// </summary>
public partial class ReturnStatement : Production
{
    /// <summary>
    /// The AST node produced on a successful match.
    /// <c>null</c> until <see cref="Init"/> fires successfully.
    /// </summary>
    public ReturnNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            // Fast-path rejection — if this isn't a return keyword we're done immediately.
            // Guards against being called speculatively from an AnyOf or similar.
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.Return)
                return new Match(false, pointer, pointer);

            // Step past the `return` keyword itself before looking for an expression.
            int current = pointer + 1;
            current = SkipTrivia(tokens, current);

            Expression? expression = null;

            // The return expression is optional — `return;` is valid PHP.
            // We attempt a full expression parse and only record it if it succeeds,
            // leaving `expression` null for the bare-return case. The node's
            // Expression property mirrors this nullability so callers can distinguish
            // the two forms without inspecting the token range directly.
            var expr = new Expression();
            var exprMatch = expr.Init()(tokens, source, current);
            if (exprMatch.Success)
            {
                expression = expr;
                current = exprMatch.End;
            }

            // Consume a trailing semicolon if present. Optional for the same reason
            // as in VariableAssignment — the top-level loop skips ExpressionTerminator
            // tokens anyway, but consuming it here keeps the node's RangeEnd accurate
            // and prevents it from being processed a second time.
            current = SkipTrivia(tokens, current);
            if (current < tokens.Length && tokens[current].Kind == TokenKind.ExpressionTerminator)
                current++;

            Node = new ReturnNode
            {
                Expression = expression?.Node,
                RangeStart = pointer,
                RangeEnd   = current
            };

            return new Match(true, pointer, current);
        };

    /// <summary>
    /// Advances <paramref name="pointer"/> past any whitespace or newline tokens,
    /// returning the updated position. Used between the <c>return</c> keyword,
    /// the optional expression, and the optional semicolon so formatting differences
    /// don't affect whether the production matches.
    ///
    /// <para>
    /// Duplicated from <see cref="VariableAssignment"/> by design — each production
    /// is self-contained, and sharing a static helper via a common base would couple
    /// productions that are otherwise independent. If trivia-skipping logic ever
    /// needs to change (e.g. to also skip comments), updating it in both places is
    /// a small cost for keeping the productions decoupled.
    /// </para>
    /// </summary>
    private static int SkipTrivia(in ReadOnlySpan<Token> tokens, int pointer)
    {
        while (pointer < tokens.Length &&
               (tokens[pointer].Kind == TokenKind.Whitespace || tokens[pointer].Kind == TokenKind.NewLine))
        {
            pointer++;
        }

        return pointer;
    }
}