using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.Runtime;
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
            // 1. THE NUD PHASE: Delegate to the main Parser dispatcher
            // This handles variables, literals, or parenthesized groups as the base.
            var left = Parser.ParseSingle(ref ctx);
            if (left == null)
            {
                result = null;
                return false;
            }

            // 2. THE LOD PHASE (The Climber)
            while (!ctx.IsAtEnd)
            {
                var token = ctx.Peek();
                var (precedence, isPostfix, isRightAssoc) = GetOperatorInfo(token.Kind);

                // If the next operator isn't strong enough to "pull" the current node,
                // we return the current 'left' up to the previous caller.
                if (precedence <= _minPrecedence) break;

                ctx.Consume(); // Take the operator token

                if (isPostfix)
                {
                    // Postfix ops (like $i++) bind immediately to the left node
                    left = new PostfixExpressionNode(left, token);
                }
                else
                {
                    // Infix: If Right-Associative (like =), we subtract 1 so the 
                    // recursive call can "steal" the token if it has the same precedence.
                    int nextMin = isRightAssoc ? precedence - 1 : precedence;
             
                    // DESCEND: Recurse with the new minimum precedence
                    if (new InnerExpressionPattern(nextMin).TryMatch(ref ctx, out var right))
                    {
                        left = new BinaryOpNode()
                        {
                            Left = left,
                            Right = right,
                            Operator = token.Kind
                        };
                    }
                    else
                    {
                        // Found operator but the right side was invalid
                        result = null;
                        return false;
                    }
                }
            }

            result = left;
            return true;
        }

        /// <summary>
        /// Maps TokenKinds to their PHP Precedence levels.
        /// Higher number = Binds tighter.
        /// </summary>
        private (int Precedence, bool IsPostfix, bool IsRightAssoc) GetOperatorInfo(TokenKind kind) => kind switch
        {
            // Precedence 10: Assignment (Right-Associative)
            TokenKind.AssignEquals or TokenKind.AddAssign or TokenKind.SubtractAssign => (10, false, true),
        
            // Precedence 20: Logical OR
            TokenKind.LogicalOr => (20, false, false),
        
            // Precedence 30: Logical AND
            TokenKind.LogicalAnd => (30, false, false),

            // Precedence 35: Comparison
            TokenKind.LessThan or TokenKind.GreaterThan or TokenKind.ShallowEquality or TokenKind.DeepEquality => (35, false, false),

            // Precedence 40: Additive
            TokenKind.Add or TokenKind.Subtract => (40, false, false),
        
            // Precedence 50: Multiplicative
            TokenKind.Multiply or TokenKind.DivideBy => (50, false, false),
        
            // Precedence 100: Postfix
            TokenKind.Increment or TokenKind.Decrement => (100, true, false),
        
            _ => (0, false, false)
        };
    }

    public class PrefixExpressionNode : SyntaxNode
    {
        public Token Operator { get; }
        public SyntaxNode Operand { get; }

        public PrefixExpressionNode(Token op, SyntaxNode operand)
        {
            Operator = op;
            Operand = operand;
        }
    }

    public class PostfixExpressionNode : SyntaxNode
    {
        public SyntaxNode Operand { get; }
        public Token Operator { get; }

        public PostfixExpressionNode(SyntaxNode operand, Token op)
        {
            Operand = operand;
            Operator = op;
        }
    }

    public partial class ExpressionCollection
    {
        /// <summary>
        /// The standard Precedence Climber.
        /// </summary>
        public InnerExpressionPattern Inner(int minPrecedence = 0)
        {
            return new InnerExpressionPattern(minPrecedence);
        }
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static readonly ExpressionCollection Expressions = new();
    }   
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitPostfixExpressionNode(PostfixExpressionNode node, in ReadOnlySpan<char> source);
        
        void VisitPrefixExpressionNode(PrefixExpressionNode node, in ReadOnlySpan<char> source);
    }
}