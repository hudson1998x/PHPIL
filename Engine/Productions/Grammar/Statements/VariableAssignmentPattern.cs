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

            // 1. Match the Variable ($var)
            if (ctx.Peek().Kind != TokenKind.Variable) return false;
            var targetToken = ctx.Consume();

            // Skip any whitespace between $var and =
            SkipTrivia(ref ctx);

            // 2. Match the Assignment Operator (=)
            if (ctx.Peek().Kind != TokenKind.AssignEquals)
            {
                ctx.Restore(start);
                return false;
            }
            ctx.Consume();

            // Skip any whitespace between = and expression
            SkipTrivia(ref ctx);

            // 3. Match the Expression
            var expr = new InnerExpressionPattern(0);
            if (!expr.TryMatch(ref ctx, out var valueNode))
            {
                ctx.Restore(start);
                return false;
            }

            // Skip any whitespace before ;
            SkipTrivia(ref ctx);

            // 4. Match the Semicolon
            if (ctx.Peek().Kind != TokenKind.ExpressionTerminator)
            {
                // In some contexts (like for loops), the terminator might be different,
                // but for a standard statement assignment, we expect ';'
                ctx.Restore(start);
                return false;
            }
            ctx.Consume();

            result = new BinaryOpNode
            {
                Left = new VariableNode { Token = targetToken },
                Operator = TokenKind.AssignEquals,
                Right = valueNode!,
                RangeStart = start,
                RangeEnd = ctx.Position
            };

            return true;
        }

        private void SkipTrivia(ref ParserContext ctx)
        {
            while (!ctx.IsAtEnd && (ctx.Peek().Kind == TokenKind.Whitespace || ctx.Peek().Kind == TokenKind.NewLine))
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
        public static VariableAssignmentPattern VariableAssignment()
        {
            return new VariableAssignmentPattern();
        }
    }
}