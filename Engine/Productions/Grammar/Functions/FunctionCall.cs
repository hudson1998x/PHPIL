using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns
{
    public class FunctionCallPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();

            // 1. Peek for Identifier (Function Name)
            var nameToken = ctx.Peek();
            if (nameToken.Kind != TokenKind.Identifier)
            {
                return false;
            }
            ctx.Consume();

            // 2. Peek for the Argument List start '('
            // If we have an identifier but no '(', it's not a call (e.g. a constant)
            if (ctx.Peek().Kind != TokenKind.LeftParen)
            {
                ctx.Restore(start);
                return false;
            }

            // 3. Delegate to ArgumentListPattern for the balanced () and args
            var argPattern = new ArgumentListPattern();
            if (!argPattern.TryMatch(ref ctx, out var argsNode) || argsNode is not ArgumentListNode list)
            {
                ctx.Restore(start);
                return false;
            }

            // 4. Construct the final ExpressionNode
            result = new FunctionCallNode
            {
                Callee = new LiteralNode()
                {
                    Token = nameToken,
                    RangeStart = start,
                    RangeEnd = list.Arguments.Count != 0 ? list.Arguments.Last().RangeEnd : start + 1
                },
                Args = list.Arguments
            };

            return true;
        }
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static FunctionCallPattern FunctionCall()
        {
            return new FunctionCallPattern();
        }
    }
}