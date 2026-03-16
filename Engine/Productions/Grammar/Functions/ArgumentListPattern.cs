using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.Visitors;


namespace PHPIL.Engine.Productions.Patterns
{
    public class ArgumentListPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            var args = new List<ExpressionNode>();
            int start = ctx.Save();

            // 1. Match '('
            if (ctx.Peek().Kind != TokenKind.LeftParen)
            {
                result = null;
                return false;
            }
            ctx.Consume();

            // 2. Parse arguments until ')'
            while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightParen)
            {
                // Check for spread operator
                bool isSpread = ctx.Peek().Kind == TokenKind.CollectSpread;
                if (isSpread)
                {
                    ctx.Consume(); // Consume '...'
                }

                // This will now handle Variables, Literals, or even nested Function Calls
                // because Inner(0) calls Parser.ParseSingle.
                var exprPattern = new InnerExpressionPattern(0);
                if (exprPattern.TryMatch(ref ctx, out var argNode) && argNode is ExpressionNode expr)
                {
                    if (isSpread)
                    {
                        // Wrap in SpreadNode
                        var spreadNode = new SpreadNode { Expression = expr };
                        args.Add(spreadNode);
                    }
                    else
                    {
                        args.Add(expr);
                    }
                }
                else
                {
                    ctx.Restore(start);
                    result = null;
                    return false;
                }

                // Handle commas and trailing commas
                if (ctx.Peek().Kind == TokenKind.Comma)
                {
                    ctx.Consume();
                    if (ctx.Peek().Kind == TokenKind.RightParen) break;
                }
            }

            // 3. Match ')'
            if (ctx.Peek().Kind != TokenKind.RightParen)
            {
                ctx.Restore(start);
                result = null;
                return false;
            }
            ctx.Consume();

            result = new ArgumentListNode { Arguments = args };
            return true;
        }
    }

    public class ArgumentListNode : SyntaxNode 
    { 
        public List<ExpressionNode> Arguments { get; init; } = [];
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitArgumentListNode(ArgumentListNode node, in ReadOnlySpan<char> source);
    }
}

