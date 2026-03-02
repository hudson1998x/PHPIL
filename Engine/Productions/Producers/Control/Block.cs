using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches a brace-delimited block of statements — <c>{ stmt; stmt; ... }</c> —
/// and produces a <see cref="BlockNode"/> containing the parsed statement list.
///
/// <para>
/// Blocks are the recursive backbone of the AST: function bodies, loop bodies,
/// if branches, and anonymous scopes all parse through here. The implementation
/// deliberately mirrors the top-level loop in <c>Parser.Parse</c>, and delegates
/// back to <c>Parser.TryProduce</c> for each statement inside the block, so any
/// construct the top-level parser understands is automatically valid inside a block
/// too — including nested blocks, functions, and loops.
/// </para>
/// </summary>
public class Block : Production
{
    /// <summary>
    /// The AST node produced on a successful match.
    /// <c>null</c> until <see cref="Init"/> fires successfully.
    /// </summary>
    public BlockNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            // A block must open with `{` — fast-path rejection for anything else.
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.LeftBrace)
                return new Match(false, pointer, pointer);

            int current = pointer + 1; // step past the opening brace
            var body    = new BlockNode { RangeStart = pointer };

            while (current < tokens.Length)
            {
                // Skip trivia and punctuation that carries no semantic meaning at the
                // statement level. Comments and semicolons are handled here rather than
                // inside TryProduce so individual productions don't each need to worry
                // about leading noise — they always receive a meaningful token.
                if (tokens[current].Kind is TokenKind.Whitespace or TokenKind.NewLine
                    or TokenKind.SingleLineComment or TokenKind.MultiLineComment
                    or TokenKind.ExpressionTerminator)
                {
                    current++;
                    continue;
                }

                // Closing brace — end of block. Consume it so `current` lands just
                // past the `}`, which becomes the Match.End for the caller. This means
                // the parent production (e.g. a loop or function) doesn't need to
                // manually step over the closing brace after Block succeeds.
                if (tokens[current].Kind == TokenKind.RightBrace)
                {
                    current++;
                    break;
                }

                // Delegate statement parsing back to the top-level dispatcher.
                // This is the key to recursion: TryProduce handles the same set of
                // constructs at every nesting depth, so `if`, `while`, function
                // declarations, assignments etc. all work identically inside a block
                // as they do at the top level. `current` is passed by ref so the
                // dispatcher can advance it past however many tokens the statement consumed.
                var node = Parser.TryProduce(in tokens, in source, tokens[current].Kind, ref current);

                if (node is not null)
                    body.Statements.Add(node);
                else
                    current++; // nothing recognised this token — skip it rather than
                               // hard-failing, keeping the parser resilient to unknown syntax
            }

            body.RangeEnd = current;
            Node          = body;

            return new Match(true, pointer, current);
        };
}