using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns
{
    public class LiteralPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            var token = ctx.Peek();
            result = null;

            switch (token.Kind)
            {
                case TokenKind.IntLiteral:
                case TokenKind.FloatLiteral:
                case TokenKind.StringLiteral:
                case TokenKind.TrueLiteral:
                case TokenKind.FalseLiteral:
                case TokenKind.NullLiteral:
                    result = new LiteralNode
                    {
                        Token = ctx.Consume(),
                        RangeStart = ctx.Position - 1,
                        RangeEnd = ctx.Position
                    };
                    return true;

                default:
                    return false;
            }
        }
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static LiteralPattern Literal()
        {
            return new LiteralPattern();
        }
    }
}