using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Producers;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions;

/// <summary>
/// The top-level parser. Consumes a flat token stream produced by the lexer and
/// builds a <see cref="SyntaxNode"/> tree that the rest of the engine can work with.
/// </summary>
public static class Parser
{
    /// <summary>
    /// Entry point for parsing. Walks the token stream from <paramref name="startPosition"/>
    /// and attempts to reduce each meaningful token into a <see cref="SyntaxNode"/>,
    /// collecting the results into a root <see cref="BlockNode"/>.
    /// </summary>
    /// <param name="tokens">The flat, ordered token stream from the lexer.</param>
    /// <param name="source">The original source text — passed through to productions so
    /// they can slice out raw string values (identifiers, literals, etc.).</param>
    /// <param name="startPosition">Token index to begin parsing from. Defaults to 0 so
    /// callers parsing a full file don't need to think about it, but recursive or
    /// partial parses can start mid-stream.</param>
    /// <returns>A <see cref="BlockNode"/> whose <c>Statements</c> list contains every
    /// top-level statement found in the token stream.</returns>
    public static SyntaxNode Parse(in ReadOnlySpan<Token> tokens, in ReadOnlySpan<char> source, int startPosition = 0)
    {
        var root    = new BlockNode();
        int pointer = startPosition;

        while (pointer < tokens.Length)
        {
            var token = tokens[pointer];

            // Skip tokens that carry no semantic meaning at the statement level.
            // PHP open/close tags, semicolons, comments, and whitespace are all noise
            // from the parser's perspective — they've already done their job in the lexer.
            if (token.Kind is TokenKind.Whitespace       or TokenKind.NewLine
                           or TokenKind.SingleLineComment or TokenKind.MultiLineComment
                           or TokenKind.PhpOpenTag        or TokenKind.PhpCloseTag
                           or TokenKind.ExpressionTerminator)
            {
                pointer++;
                continue;
            }

            // Dispatch to the appropriate production rule based on the leading token.
            // The production updates `pointer` to just past whatever it consumed, so
            // the next iteration starts on a fresh token.
            var node = TryProduce(tokens, source, token.Kind, ref pointer);

            if (node is not null)
                root.Statements.Add(node);
            else
                // Nothing recognised this token — skip it rather than hard-failing so
                // the parser stays resilient against unknown or unsupported syntax.
                pointer++;
        }

        return root;
    }

    /// <summary>
    /// Attempts to match and produce a <see cref="SyntaxNode"/> beginning at
    /// <paramref name="pointer"/>, guided by the leading <paramref name="kind"/>.
    /// On success, <paramref name="pointer"/> is advanced past all consumed tokens.
    /// On failure, <paramref name="pointer"/> is left unchanged and <c>null</c> is returned,
    /// letting the caller decide how to recover.
    /// </summary>
    /// <param name="tokens">The full token stream.</param>
    /// <param name="source">The raw source text.</param>
    /// <param name="kind">The kind of the token at <paramref name="pointer"/> — used as a
    /// fast dispatch key so we avoid instantiating a production that has no chance of matching.</param>
    /// <param name="pointer">Current read position; updated in-place on success.</param>
    /// <returns>The matched <see cref="SyntaxNode"/>, or <c>null</c> if no production matched.</returns>
    public static SyntaxNode? TryProduce(
        in ReadOnlySpan<Token> tokens,
        in ReadOnlySpan<char> source,
        TokenKind kind,
        ref int pointer)
    {
        switch (kind)
        {
            // `if` gets its own dedicated production because it has optional `elseif`/`else`
            // branches that a generic keyword-block rule can't handle.
            case TokenKind.If:
            {
                var production = new IfExpression();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success) { pointer = match.End; return production.Node; }
                break;
            }

            // `while`, `for`, and `foreach` all share the same shape:
            // keyword → parenthesised expression → block body.
            case TokenKind.While:
            case TokenKind.For:
            case TokenKind.Foreach:
            {
                var production = new KeywordExpressionBlock();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success) { pointer = match.End; return production.Node; }
                break;
            }

            // A bare `{` signals an anonymous block scope.
            case TokenKind.LeftBrace:
            {
                var production = new Block();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success) { pointer = match.End; return production.Node; }
                break;
            }
            
            case TokenKind.Function:
            {
                // Try named function declaration first — if that fails (e.g. no identifier
                // follows the keyword), fall back to an anonymous function/closure expression.
                var named = new FunctionDeclaration();
                var namedMatch = named.Init()(tokens, source, pointer);
                if (namedMatch.Success) { pointer = namedMatch.End; return named.Node; }

                var anon = new AnonymousFunction();
                var anonMatch = anon.Init()(tokens, source, pointer);
                if (anonMatch.Success) { pointer = anonMatch.End; return anon.Node; }

                break;
            }
            
            case TokenKind.Variable:
            {
                // A variable token can open either an assignment (`$x = ...`) or a
                // standalone expression (`$x->method()`, `$arr[0]`, etc.).
                // Try the more specific rule first; fall back to the general expression
                // rule only when there's no assignment operator following the variable.
                var assignment = new VariableAssignment();
                var assignMatch = assignment.Init()(tokens, source, pointer);

                if (assignMatch.Success)
                {
                    pointer = assignMatch.End;
                    return assignment.Node;
                }

                // Not an assignment — treat the variable as the start of an expression.
                var expression = new Expression();
                var exprMatch  = expression.Init()(tokens, source, pointer);

                if (exprMatch.Success)
                {
                    pointer = exprMatch.End;
                    return expression.Node;
                }

                break;
            }
            
            // `return` is a statement, but it wraps an optional expression, so it has
            // its own production rather than being lumped in with the generic expression set.
            case TokenKind.Return:
            {
                var production = new ReturnStatement();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success)
                {
                    pointer = match.End;
                    return production.Node;
                }
                break;
            }

            // Everything else that can legally open an expression statement.
            // This covers literals, identifiers (function calls, constants), parenthesised
            // sub-expressions, unary operators, and the handful of language-level keywords
            // that behave like functions (`echo`, `print`, `include`, etc.).
            case TokenKind.Identifier:
            case TokenKind.IntLiteral:
            case TokenKind.FloatLiteral:
            case TokenKind.StringLiteral:
            case TokenKind.TrueLiteral:
            case TokenKind.FalseLiteral:
            case TokenKind.NullLiteral:
            case TokenKind.LeftParen:
            case TokenKind.Not:
            case TokenKind.Subtract:
            case TokenKind.Increment:
            case TokenKind.Decrement:
            case TokenKind.Print:
            case TokenKind.Echo:
            case TokenKind.Include:
            case TokenKind.IncludeOnce:
            case TokenKind.Require:
            case TokenKind.RequireOnce:
            {
                var production = new Expression();
                var match      = production.Init()(tokens, source, pointer);
                if (match.Success) { pointer = match.End; return production.Node; }
                break;
            }
            default:
                throw new InvalidOperationException($"Unexpected '{tokens[pointer].Kind}'");
        }

        // No production claimed this token.
        return null;
    }
}