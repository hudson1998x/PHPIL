using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure.Loops;

namespace PHPIL.Engine.Productions.Patterns;

public class ForeachExpressionPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        int start = ctx.Save();

        // Match 'foreach'
        if (ctx.Peek().Kind != TokenKind.Foreach)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        // Match '('
        if (ctx.Peek().Kind != TokenKind.LeftParen)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        // --- Match iterable expression ---
        SyntaxNode? iterableNode = null;
        if (!Grammar.Expressions.Outer().TryMatch(ref ctx, out iterableNode))
            Grammar.Expressions.Inner().TryMatch(ref ctx, out iterableNode);
        SkipTrivia(ref ctx);

        // Match 'as'
        if (ctx.Peek().Kind != TokenKind.As)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        // --- Match key => value or just value ---
        VariableNode? keyNode = null;
        VariableNode? valueNode = null;

        if (ctx.Peek().Kind == TokenKind.Variable)
        {
            // Tentatively parse first variable
            int varStart = ctx.Save();
            var firstVar = new VariableNode { Token = ctx.Peek() };
            ctx.Consume();
            SkipTrivia(ref ctx);

            // Check for '=>'
            if (ctx.Peek().Kind == TokenKind.Arrow)
            {
                ctx.Consume();
                SkipTrivia(ref ctx);

                if (ctx.Peek().Kind != TokenKind.Variable)
                {
                    ctx.Restore(start);
                    return false;
                }

                keyNode = firstVar;
                valueNode = new VariableNode { Token = ctx.Peek() };
                ctx.Consume();
            }
            else
            {
                valueNode = firstVar;
            }
        }
        else
        {
            ctx.Restore(start);
            return false;
        }

        SkipTrivia(ref ctx);

        // Match ')'
        if (ctx.Peek().Kind != TokenKind.RightParen)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        // --- Match body block ---
        var blockPattern = new BlockPattern();
        if (!blockPattern.TryMatch(ref ctx, out var bodyNode))
        {
            ctx.Restore(start);
            return false;
        }

        // Build Foreach node
        result = new ForeachNode
        {
            Iterable = iterableNode as ExpressionNode,
            Key      = keyNode,
            Value    = valueNode,
            Body     = (BlockNode?)bodyNode
        };

        return true;
    }

    private void SkipTrivia(ref ParserContext ctx)
    {
        while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
            ctx.Consume();
    }
}

