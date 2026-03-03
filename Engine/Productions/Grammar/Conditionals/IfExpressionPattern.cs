using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class IfExpressionPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        int start = ctx.Save();

        // Match 'if'
        if (ctx.Peek().Kind != TokenKind.If)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        // Match condition '(' expr ')'
        if (ctx.Peek().Kind != TokenKind.LeftParen)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        var climber = new InnerExpressionPattern(0);
        if (!climber.TryMatch(ref ctx, out var condition))
        {
            ctx.Restore(start);
            return false;
        }
        SkipTrivia(ref ctx);

        if (ctx.Peek().Kind != TokenKind.RightParen)
        {
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();
        SkipTrivia(ref ctx);

        // Match body block
        var blockPattern = new BlockPattern();
        if (!blockPattern.TryMatch(ref ctx, out var bodyNode))
        {
            ctx.Restore(start);
            return false;
        }

        // Collect elseif / else chains
        var elseIfs = new List<ElseIfNode>();
        ElseNode? elseNode = null;

        while (!ctx.IsAtEnd)
        {
            SkipTrivia(ref ctx);
            if (ctx.IsAtEnd) break;

            // Try elseif
            if (ctx.Peek().Kind == TokenKind.ElseIf)
            {
                int elseIfStart = ctx.Save();
                ctx.Consume();
                SkipTrivia(ref ctx);

                if (ctx.Peek().Kind != TokenKind.LeftParen)
                {
                    ctx.Restore(elseIfStart);
                    break;
                }
                ctx.Consume();
                SkipTrivia(ref ctx);

                if (!new InnerExpressionPattern(0).TryMatch(ref ctx, out var elseIfCondition))
                {
                    ctx.Restore(elseIfStart);
                    break;
                }
                SkipTrivia(ref ctx);

                if (ctx.Peek().Kind != TokenKind.RightParen)
                {
                    ctx.Restore(elseIfStart);
                    break;
                }
                ctx.Consume();
                SkipTrivia(ref ctx);

                if (!new BlockPattern().TryMatch(ref ctx, out var elseIfBody))
                {
                    ctx.Restore(elseIfStart);
                    break;
                }

                elseIfs.Add(new ElseIfNode
                {
                    Expression = elseIfCondition as ExpressionNode,
                    Body = (BlockNode?)elseIfBody
                });

                continue;
            }

            // Try else
            if (ctx.Peek().Kind == TokenKind.Else)
            {
                int elseStart = ctx.Save();
                ctx.Consume();
                SkipTrivia(ref ctx);

                if (!new BlockPattern().TryMatch(ref ctx, out var elseBody))
                {
                    ctx.Restore(elseStart);
                    break;
                }

                elseNode = new ElseNode
                {
                    Body = (BlockNode?)elseBody
                };

                break; // else is always last
            }

            break;
        }

        result = new IfNode
        {
            Expression = condition as ExpressionNode,
            Body = (BlockNode?)bodyNode,
            ElseIfs = elseIfs,
            ElseNode = elseNode
        };

        return true;
    }

    private void SkipTrivia(ref ParserContext ctx)
    {
        while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
            ctx.Consume();
    }
}