using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Producers;

/// <summary>
/// Matches a named PHP function declaration of the form:
/// <code>
/// function name(TypeHint $param, ...) : ReturnType { ... }
/// </code>
/// and produces a <see cref="FunctionNode"/> capturing the name, parameter list,
/// optional return type annotation, and body block.
///
/// <para>
/// Written imperatively rather than as a combinator chain for the same reasons as
/// <see cref="VariableAssignment"/> and <see cref="ReturnStatement"/> — the parameter
/// list is variable-length and needs to accumulate into a <see cref="List{T}"/>,
/// which doesn't map cleanly onto <c>Repeated</c> + <c>Capture</c> without
/// significantly more ceremony. The imperative loop is easier to follow and debug.
/// </para>
/// </summary>
public class FunctionDeclaration : Production
{
    /// <summary>
    /// The AST node produced on a successful match.
    /// <c>null</c> until <see cref="Init"/> fires successfully.
    /// </summary>
    public FunctionNode? Node { get; private set; }

    public override Producer Init() =>
        (in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int pointer) =>
        {
            // Fast-path rejection — must open with the `function` keyword.
            if (pointer >= tokens.Length || tokens[pointer].Kind != TokenKind.Function)
                return new Match(false, pointer, pointer);

            int current = pointer + 1;
            current = SkipTrivia(tokens, current);

            // Function name — anonymous functions are not handled here; they surface
            // as expressions and are parsed by the Expression production instead.
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.Identifier)
                return new Match(false, pointer, pointer);

            var nameToken = tokens[current];
            current++;
            current = SkipTrivia(tokens, current);

            // Opening paren — mandatory, no paren means malformed input.
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.LeftParen)
                return new Match(false, pointer, pointer);

            current++;
            current = SkipTrivia(tokens, current);

            // Parameter list — iterate until we hit the closing paren.
            // Each parameter is an optional type hint followed by a required variable name,
            // with a comma separator between entries. We fail the whole production if a
            // variable token is missing mid-list, since that's unrecoverable.
            var parameters = new List<FunctionParameter>();
            while (current < tokens.Length && tokens[current].Kind != TokenKind.RightParen)
            {
                current = SkipTrivia(tokens, current);

                // Type hint is optional — PHP allows both `function foo($x)` and
                // `function foo(int $x)`. Peek at the current token: if it's an
                // identifier we treat it as the hint and advance, otherwise skip.
                Token typeHint = default;
                if (tokens[current].Kind == TokenKind.Identifier)
                {
                    typeHint = tokens[current];
                    current++;
                    current = SkipTrivia(tokens, current);
                }

                // The variable name itself is not optional — fail out if it's absent.
                if (current >= tokens.Length || tokens[current].Kind != TokenKind.Variable)
                    return new Match(false, pointer, pointer);

                var paramName = tokens[current];
                current++;
                current = SkipTrivia(tokens, current);

                parameters.Add(new FunctionParameter
                {
                    Name     = paramName,
                    TypeHint = typeHint   // default(Token) when no hint was present
                });

                // Consume the comma between parameters. Not finding one here is fine —
                // the while condition will see the RightParen on the next iteration
                // and exit cleanly.
                if (current < tokens.Length && tokens[current].Kind == TokenKind.Comma)
                {
                    current++;
                    current = SkipTrivia(tokens, current);
                }
            }

            // Closing paren — we consumed every parameter token above, so failing
            // here means the source is malformed (unclosed parameter list).
            if (current >= tokens.Length || tokens[current].Kind != TokenKind.RightParen)
                return new Match(false, pointer, pointer);

            current++;
            current = SkipTrivia(tokens, current);

            // Return type annotation — PHP 7+ syntax: `: TypeName` after the parameter
            // list. Entirely optional; `returnType` stays as `default(Token)` if absent,
            // and the node stores that sentinel so downstream code can test for it.
            Token returnType = default;
            if (current < tokens.Length && tokens[current].Kind == TokenKind.Colon)
            {
                current++;
                current = SkipTrivia(tokens, current);

                // The colon must be followed by a type identifier — a trailing colon
                // with no type is silently ignored rather than failing, which is
                // lenient but avoids rejecting partially-formed code during editing.
                if (current < tokens.Length && tokens[current].Kind == TokenKind.Identifier)
                {
                    returnType = tokens[current];
                    current++;
                    current = SkipTrivia(tokens, current);
                }
            }

            // Body block — delegate to the Block production for the braced statement list.
            // This is the one sub-production we can't handle inline, since blocks are
            // recursive (a function body may contain nested functions, loops, etc.).
            var block = new Block();
            var blockMatch = block.Init()(tokens, source, current);
            if (!blockMatch.Success)
                return new Match(false, pointer, pointer);

            current = blockMatch.End;

            Node = new FunctionNode
            {
                Name       = nameToken,
                Params     = parameters,
                ReturnType = returnType,  // default(Token) if no return type was declared
                Body       = block.Node,
                RangeStart = pointer,
                RangeEnd   = current
            };

            return new Match(true, pointer, current);
        };

    /// <summary>
    /// Advances <paramref name="pointer"/> past any whitespace or newline tokens,
    /// returning the updated position. Called between every logical section of the
    /// declaration so formatting variation (spaces, line breaks between parameters,
    /// indentation before the opening brace, etc.) doesn't affect whether the
    /// production matches.
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