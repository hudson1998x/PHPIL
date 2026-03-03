using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Productions;

public static class Parser
{
    public static SyntaxNode? Parse(in Token[] tokens, in ReadOnlySpan<char> source)
    {
        var ctx = new ParserContext(tokens.AsSpan(), source);
        var root = new BlockNode();

        while (!ctx.IsAtEnd)
        {
            var kind = ctx.Peek().Kind;
            if (kind is TokenKind.Whitespace or TokenKind.NewLine or TokenKind.PhpOpenTag)
            {
                ctx.Consume();
                continue;
            }

            var node = ParseSingle(ref ctx);
            if (node != null)
            {
                root.Statements.Add(node);
            }
            else if (!ctx.IsAtEnd)
            {
                ctx.Consume();
            }
        }
        return root;
    }

    public static SyntaxNode? ParseSingle(ref ParserContext ctx)
    {
        SkipTrivia(ref ctx);
        if (ctx.IsAtEnd) return null;

        var kind = ctx.Peek().Kind;
        SyntaxNode? result = null;

        switch (kind)
        {
            case TokenKind.Function:
                // Forced match: if it's 'function', it's a declaration.
                if (Grammar.FunctionDeclaration().TryMatch(ref ctx, out result)) return result;
                if (Grammar.AnonymousFunction().TryMatch(ref ctx, out result)) return result;
                throw new Exception($"Syntax Error: Failed to parse function at {ctx.Peek().RangeStart}");

            case TokenKind.Variable:
                if (Grammar.VariableAssignment().TryMatch(ref ctx, out result)) return result;
                return new VariableNode { Token = ctx.Consume() };

            case TokenKind.Identifier:
                if (Grammar.FunctionCall().TryMatch(ref ctx, out result)) return result;
                // If it's not a call, it's just a loose identifier
                return new IdentifierNode { Token = ctx.Consume() };

            case TokenKind.LeftBrace:
                if (Grammar.Block().TryMatch(ref ctx, out result)) return result;
                break;
                
            default:
                if (Grammar.Literal().TryMatch(ref ctx, out result)) return result;
                break;
        }

        return result;
    }

    private static void SkipTrivia(ref ParserContext ctx)
    {
        while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
        {
            ctx.Consume();
        }
    }
}