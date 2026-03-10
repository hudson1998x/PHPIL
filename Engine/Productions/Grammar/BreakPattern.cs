using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns
{
    public class BreakPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();

            SkipTrivia(ref ctx);

            if (ctx.Peek().Kind != TokenKind.Break)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume();

            SkipTrivia(ref ctx);

            // Optional level: break 2;
            Token? label = null;
            if (ctx.Peek().Kind == TokenKind.IntLiteral)
                label = ctx.Consume();

            SkipTrivia(ref ctx);

            // Consume ';'
            if (ctx.Peek().Kind != TokenKind.ExpressionTerminator)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume();

            result = new BreakNode { Label = label };
            return true;
        }

        private void SkipTrivia(ref ParserContext ctx)
        {
            while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
                ctx.Consume();
        }
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static BreakPattern Break() => new BreakPattern();
    }
}