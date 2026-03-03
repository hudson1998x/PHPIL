using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns
{
    public class PostfixExpressionPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            int start = ctx.Save();

            if (ctx.Peek().Kind == TokenKind.Variable)
            {
                var varNode = new VariableNode { Token = ctx.Consume() };
                var opToken = ctx.Peek();

                if (opToken.Kind == TokenKind.Increment || opToken.Kind == TokenKind.Decrement)
                {
                    ctx.Consume(); // Consume ++ or --
                    result = new PostfixExpressionNode(varNode, opToken);
                    return true;
                }
            }

            ctx.Restore(start);
            result = null;
            return false;
        }
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static PostfixExpressionPattern PostfixExpression() => new();
    }   
}