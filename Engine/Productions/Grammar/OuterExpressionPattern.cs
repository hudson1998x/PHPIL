using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class OuterExpressionPattern : Pattern
{
    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        result = null;
        int start = ctx.Save();

        // 1. Check for opening '('
        if (ctx.Peek().Kind != TokenKind.LeftParen)
        {
            return false;
        }
        ctx.Consume();

        // 2. Restart the Inner climber at 0 precedence
        var inner = new InnerExpressionPattern(0);
        if (!inner.TryMatch(ref ctx, out var expression))
        {
            ctx.Restore(start);
            return false;
        }

        // 3. Check for closing ')'
        if (ctx.Peek().Kind != TokenKind.RightParen)
        {
            // Unbalanced parens
            ctx.Restore(start);
            return false;
        }
        ctx.Consume();

        // Return the expression directly to keep the tree lean
        result = expression;
        return true;
    }
}

public partial class ExpressionCollection
{
    /// <summary>
    /// The standard Precedence Climber.
    /// </summary>
    public OuterExpressionPattern Outer()
    {
        return new OuterExpressionPattern();
    }
}