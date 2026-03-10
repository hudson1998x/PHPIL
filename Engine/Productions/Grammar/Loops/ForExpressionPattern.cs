using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class ForExpressionPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        int start = ctx.Save();

        // Match 'for'
        if (ctx.Peek().Kind != TokenKind.For)
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

        // --- Match initializer (may be VariableDeclaration or expression) ---
        SyntaxNode? initNode = null;
        if (ctx.Peek().Kind != TokenKind.ExpressionTerminator)
            Grammar.Expressions.Inner().TryMatch(ref ctx, out initNode);

        // Match ';'
        if (ctx.Peek().Kind != TokenKind.ExpressionTerminator)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        // --- Match condition ---
        SyntaxNode? conditionNode = null;
        if (ctx.Peek().Kind != TokenKind.ExpressionTerminator)
            Grammar.Expressions.Inner().TryMatch(ref ctx, out conditionNode);

        // Match ';'
        if (ctx.Peek().Kind != TokenKind.ExpressionTerminator)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        // --- Match increment ---
        SyntaxNode? incrementNode = null;
        if (ctx.Peek().Kind != TokenKind.RightParen)
            Grammar.Expressions.Inner().TryMatch(ref ctx, out incrementNode);

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

        // Consume any trailing expression terminators after the body
        while (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.ExpressionTerminator)
            ctx.Consume();

        result = new For
        {
            Init      = initNode,
            Condition = conditionNode as ExpressionNode,
            Increment = incrementNode as ExpressionNode,
            Body      = (BlockNode?)bodyNode
        };

        return true;
    }

    private void SkipTrivia(ref ParserContext ctx)
    {
        while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
            ctx.Consume();
    }
}