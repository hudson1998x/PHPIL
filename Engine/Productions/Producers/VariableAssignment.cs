using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches a variable assignment statement of the form <c>$name = &lt;expr&gt;;</c>
/// and produces a <see cref="VariableDeclaration"/> node capturing the variable
/// name and its assigned value.
///
/// <para>
/// This production is intentionally written as a hand-rolled imperative parser
/// rather than a combinator chain. Variable assignment has a fixed, well-known
/// shape, and the imperative style makes it easier to thread the variable token
/// and the expression node through to the final <see cref="VariableDeclaration"/>
/// without needing multiple <c>Capture</c> callbacks closing over mutable state.
/// </para>
/// </summary>
public class VariableAssignment : Production
{
    /// <summary>
    /// The AST node produced on a successful match.
    /// <c>null</c> if <see cref="Init"/> has not yet been invoked or the match failed.
    /// </summary>
    public VariableDeclaration? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            // Fast-path rejection: if the stream doesn't open with a variable token
            // there's no point going further. This keeps the common "not an assignment"
            // case cheap — no sub-productions are allocated.
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.Variable)
                return new Match(false, pointer, pointer);

            int current = pointer;

            // Capture the variable token now so we have its source range and kind
            // available when constructing the node at the end, without having to
            // re-index into the span after advancing `current`.
            var variableToken = tokens[current];
            current++;
            current = SkipTrivia(tokens, current);

            // The assignment operator is the distinguishing feature of this production —
            // if it's absent we're looking at a standalone variable expression, not an
            // assignment, so bail out and let the Expression production handle it instead.
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.AssignEquals)
                return new Match(false, pointer, pointer);

            current++;
            current = SkipTrivia(tokens, current);

            // Delegate the right-hand side to the general Expression production.
            // This gives assignments their full expressive power — the RHS can be
            // a literal, a function call, another variable, a ternary, etc. —
            // without this production needing to know about any of that.
            var expression = new Expression();
            var exprMatch  = expression.Init()(tokens, source, current);

            if (!exprMatch.Success)
                return new Match(false, pointer, pointer);

            current = exprMatch.End;
            current = SkipTrivia(tokens, current);

            // The semicolon is optional here rather than required because the parser's
            // top-level loop also skips ExpressionTerminator tokens. Consuming it when
            // present avoids it being seen again by the outer loop, but omitting it
            // doesn't break anything.
            if (current < tokens.Length && tokens[current].Kind == TokenKind.ExpressionTerminator)
                current++;

            // All pieces are in hand — build the node.
            Node = new VariableDeclaration()
            {
                VariableName  = variableToken,
                VariableValue = expression.Node,
                RangeStart    = pointer,
                RangeEnd      = current
            };

            return new Match(true, pointer, current);
        };

    /// <summary>
    /// Advances <paramref name="pointer"/> past any interleaved whitespace or newline
    /// tokens, returning the new position. Used between the logical parts of the
    /// assignment (name, operator, expression) so the grammar accepts the full range
    /// of PHP formatting styles — compact <c>$x=1</c> and spaced <c>$x = 1</c> alike.
    ///
    /// <para>
    /// Only whitespace and newlines are skipped — comments are intentionally left
    /// for the top-level loop to discard, keeping this production focused purely
    /// on the assignment structure.
    /// </para>
    /// </summary>
    private static int SkipTrivia(in ReadOnlySpan<Token> tokens, int pointer)
    {
        while (pointer < tokens.Length &&
               tokens[pointer].Kind is TokenKind.Whitespace or TokenKind.NewLine)
        {
            pointer++;
        }

        return pointer;
    }
}