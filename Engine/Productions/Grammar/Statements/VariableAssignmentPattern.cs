using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns
{
    public class VariableAssignmentPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();
            Parser.SkipTrivia(ref ctx);

            // 1. Match variable
            if (ctx.IsAtEnd || ctx.Peek().Kind != TokenKind.Variable) return false;
            var varToken = ctx.Consume();
            Parser.SkipTrivia(ref ctx);

            // 2. Match '='
            if (ctx.IsAtEnd || ctx.Peek().Kind != TokenKind.AssignEquals)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume();
            Parser.SkipTrivia(ref ctx);

            // 3. Match RHS (array literal OR any expression)
            ExpressionNode? valueNode = null;

            if (Grammar.ArrayLiteral().TryMatch(ref ctx, out var arrayLiteral))
            {
                valueNode = arrayLiteral as ExpressionNode;
            }
            else if (!new InnerExpressionPattern(0).TryMatch(ref ctx, out var expr))
            {
                ctx.Restore(start);
                return false;
            }
            else
            {
                valueNode = expr as ExpressionNode;
            }

            Parser.SkipTrivia(ref ctx);

            // 4. Match ';'
            if (ctx.IsAtEnd || ctx.Peek().Kind != TokenKind.ExpressionTerminator)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume(); // ';'

            // 5. Build assignment node
            result = new BinaryOpNode
            {
                Left = new VariableNode { Token = varToken },
                Right = valueNode!,
                Operator = TokenKind.AssignEquals,
                RangeStart = start,
                RangeEnd = ctx.Position
            };
            return true;
        }
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static VariableAssignmentPattern VariableAssignment()
        {
            return new VariableAssignmentPattern();
        }
    }
}