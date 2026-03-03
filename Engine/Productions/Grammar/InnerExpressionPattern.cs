using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Productions.Patterns
{
    public class InnerExpressionPattern : Pattern
    {
        private readonly int _minPrecedence;

        public InnerExpressionPattern(int minPrecedence = 0) => _minPrecedence = minPrecedence;

        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            // 1. THE NUD PHASE: Must consume at least one token via ParseAtom
            var left = Parser.ParseAtom(ref ctx);
            if (left == null)
            {
                result = null;
                return false;
            }

            // 2. THE LED PHASE (The Climber)
            while (!ctx.IsAtEnd)
            {
                // *** FIX: Skip whitespace/newlines before peeking for an operator ***
                Parser.SkipTrivia(ref ctx);
                if (ctx.IsAtEnd) break;

                var token = ctx.Peek();
                var (precedence, isPostfix, isRightAssoc) = GetOperatorInfo(token.Kind);

                if (precedence <= _minPrecedence || precedence == 0) break;

                ctx.Consume();

                if (isPostfix)
                {
                    left = new PostfixExpressionNode(left, token);
                }
                else
                {
                    int nextMin = isRightAssoc ? precedence - 1 : precedence;

                    // *** FIX: Skip whitespace/newlines before parsing the right-hand side ***
                    Parser.SkipTrivia(ref ctx);

                    if (new InnerExpressionPattern(nextMin).TryMatch(ref ctx, out var right))
                    {
                        left = new BinaryOpNode()
                        {
                            Left = left as ExpressionNode,
                            Right = right as ExpressionNode,
                            Operator = token.Kind
                        };
                    }
                    else
                    {
                        break;
                    }
                }
            }

            result = left;
            return true;
        }

        private (int Precedence, bool IsPostfix, bool IsRightAssoc) GetOperatorInfo(TokenKind kind) => kind switch
        {
            TokenKind.AssignEquals or TokenKind.AddAssign or TokenKind.SubtractAssign => (10, false, true),
            TokenKind.LogicalOr => (20, false, false),
            TokenKind.LogicalAnd => (30, false, false),
            TokenKind.LessThan or TokenKind.GreaterThan or TokenKind.ShallowEquality or TokenKind.DeepEquality => (35, false, false),
            TokenKind.Add or TokenKind.Subtract or TokenKind.Concat => (40, false, false),
            TokenKind.Multiply or TokenKind.DivideBy => (50, false, false),
            TokenKind.Increment or TokenKind.Decrement => (100, true, false),
            _ => (0, false, false)
        };
    }

    public class PrefixExpressionNode : ExpressionNode
    {
        public Token Operator { get; }
        public SyntaxNode Operand { get; }

        public PrefixExpressionNode(Token op, SyntaxNode operand)
        {
            Operator = op;
            Operand = operand;
        }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
            => visitor.VisitPrefixExpressionNode(this, source);

        public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
        {
            builder.Append("{");
            builder.Append($"\"type\": \"{GetType().Name}\",");
            builder.Append("\"operator\": ");
            Operator.ToJson(in span, builder);
            builder.Append(",\"operand\": ");
            if (Operand != null) Operand.ToJson(in span, in tokens, builder);
            else builder.Append("null");
            builder.Append("}");
        }
    }

    public class PostfixExpressionNode : ExpressionNode
    {
        public SyntaxNode Operand { get; }
        public Token Operator { get; }

        public PostfixExpressionNode(SyntaxNode operand, Token op)
        {
            Operand = operand;
            Operator = op;
        }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
            => visitor.VisitPostfixExpressionNode(this, source);

        public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
        {
            builder.Append("{");
            builder.Append($"\"type\": \"{GetType().Name}\",");
            builder.Append("\"operator\": ");
            Operator.ToJson(in span, builder);
            builder.Append(",\"operand\": ");
            if (Operand != null) Operand.ToJson(in span, in tokens, builder);
            else builder.Append("null");
            builder.Append("}");
        }
    }

    public partial class ExpressionCollection
    {
        public InnerExpressionPattern Inner(int minPrecedence = 0) => new InnerExpressionPattern(minPrecedence);
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitPrefixExpressionNode(PrefixExpressionNode node, in ReadOnlySpan<char> source);
        void VisitPostfixExpressionNode(PostfixExpressionNode node, in ReadOnlySpan<char> source);
    }
}