using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns
{
    public class ReturnPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            int start = ctx.Save();
            result = null;

            // Must start with 'return'
            if (ctx.Peek().Kind != TokenKind.Return)
                return false;

            var returnToken = ctx.Consume();
            SkipTrivia(ref ctx);

            ExpressionNode? expression = null;

            // Try Outer first, then Inner if Outer fails
            if (ctx.Peek().Kind is not TokenKind.ExpressionTerminator 
                and not TokenKind.RightBrace)
            {
                if (!Grammar.Expressions.Outer().TryMatch(ref ctx, out var exprNode) &&
                    !Grammar.Expressions.Inner().TryMatch(ref ctx, out exprNode))
                {
                    ctx.Restore(start);
                    return false;
                }

                expression = (ExpressionNode)exprNode!;
                SkipTrivia(ref ctx);
            }

            // Optional semicolon
            if (ctx.Peek().Kind == TokenKind.ExpressionTerminator)
                ctx.Consume();

            result = new ReturnNode
            {
                Expression = expression
            };

            return true;
        }

        private void SkipTrivia(ref ParserContext ctx)
        {
            while (!ctx.IsAtEnd &&
                   (ctx.Peek().Kind == TokenKind.Whitespace ||
                    ctx.Peek().Kind == TokenKind.NewLine))
            {
                ctx.Consume();
            }
        }
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static ReturnPattern Return()
        {
            return new();
        }
    }
}