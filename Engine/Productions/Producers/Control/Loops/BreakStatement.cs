using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;
using System;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches a <c>break</c> statement, with an optional numeric level.
/// Produces a <see cref="BreakNode"/> whose <c>Label</c> (level) is <c>null</c>
/// for bare <c>break;</c> and populated for <c>break 2;</c>.
///
/// <para>
/// Following the pattern of <see cref="ReturnStatement"/>, this is written 
/// imperatively. It checks for the <c>break</c> keyword, optionally attempts 
/// to parse an integer literal for the nesting level, and ensures the 
/// statement is terminated correctly.
/// </para>
/// </summary>
public partial class BreakStatement : Production
{
    /// <summary>
    /// The AST node produced on a successful match.
    /// <c>null</c> until <see cref="Init"/> fires successfully.
    /// </summary>
    public BreakNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            // Fast-path rejection — if this isn't a break keyword we're done immediately.
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.Break)
                return new Match(false, pointer, pointer);

            // Step past the `break` keyword.
            int current = pointer + 1;
            current = SkipTrivia(tokens, current);

            Token? labelToken = null;

            // PHP break level is optional. We check if the next token is an integer literal.
            // Note: PHP also allows expressions here in some versions, but usually 
            // it's a literal. If you want to support 'break $var;', you'd call 
            // a full Expression producer here instead.
            if (current < tokens.Length && tokens[current].Kind == TokenKind.IntLiteral)
            {
                labelToken = tokens[current];
                current++;
            }

            // Consume a trailing semicolon if present.
            current = SkipTrivia(tokens, current);
            if (current < tokens.Length && tokens[current].Kind == TokenKind.ExpressionTerminator)
            {
                current++;
            }

            Node = new BreakNode
            {
                Label = labelToken,
                RangeStart = pointer,
                RangeEnd   = current
            };

            return new Match(true, pointer, current);
        };

    /// <summary>
    /// Advances <paramref name="pointer"/> past any whitespace or newline tokens.
    /// Internal trivia skipping ensures that 'break   2;' matches correctly.
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