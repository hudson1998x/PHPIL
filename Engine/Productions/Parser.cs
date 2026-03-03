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
            SkipTrivia(ref ctx);
            if (ctx.IsAtEnd) break;

            int startPos = ctx.Save();
            var node = ParseSingle(ref ctx);

            if (node != null)
                root.Statements.Add(node);

            SkipTrivia(ref ctx);

            if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.ExpressionTerminator)
                ctx.Consume();

            if (ctx.Save() == startPos && !ctx.IsAtEnd)
                ctx.Consume();
        }

        return root;
    }

    public static SyntaxNode? ParseSingle(ref ParserContext ctx)
    {
        SkipTrivia(ref ctx);
        if (ctx.IsAtEnd) return null;

        switch (ctx.Peek().Kind)
        {
            case TokenKind.Function:
                if (Grammar.FunctionDeclaration().TryMatch(ref ctx, out var fn)) return fn;
                break;

            case TokenKind.If:
                if (Grammar.If().TryMatch(ref ctx, out var ifNode)) return ifNode;
                break;
        }

        // The Climber (This is the only way into the expression tree)
        var climber = new InnerExpressionPattern(0);
        if (climber.TryMatch(ref ctx, out var result)) return result;

        return null;
    }

    public static SyntaxNode? ParseAtom(ref ParserContext ctx)
    {
        SkipTrivia(ref ctx);
        if (ctx.IsAtEnd) return null;

        int startPos = ctx.Save();
        var token = ctx.Peek();

        switch (token.Kind)
        {
            // --- PREFIX OPERATORS ---
            case TokenKind.Increment:
            case TokenKind.Decrement:
            {
                var op = ctx.Consume();
                var operand = ParseAtom(ref ctx);
                if (operand != null) return new PrefixExpressionNode(op, operand);
                ctx.Restore(startPos);
                return null;
            }

            // --- GROUPING ---
            case TokenKind.LeftParen:
            {
                ctx.Consume();
                var inner = ParseSingle(ref ctx);
                SkipTrivia(ref ctx);
                if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.RightParen)
                    ctx.Consume();
                return inner;
            }

            // --- IDENTIFIERS / FUNCTION CALLS ---
            case TokenKind.Identifier:
            {
                if (Grammar.FunctionCall().TryMatch(ref ctx, out var call)) return call;
                return new IdentifierNode { Token = ctx.Consume() };
            }

            // --- TERMINALS ---
            case TokenKind.Variable:
                return new VariableNode { Token = ctx.Consume() };

            case TokenKind.IntLiteral:
            case TokenKind.StringLiteral:
            {
                if (Grammar.Literal().TryMatch(ref ctx, out var lit)) return lit;
                break;
            }
        }

        // Nothing matched, nothing consumed
        return null;
    }

    internal static void SkipTrivia(ref ParserContext ctx)
    {
        while (!ctx.IsAtEnd &&
               (ctx.Peek().Kind == TokenKind.Whitespace ||
                ctx.Peek().Kind == TokenKind.NewLine))
            ctx.Consume();
    }
}