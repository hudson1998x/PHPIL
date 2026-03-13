using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Productions.Patterns
{
    /// <summary>
    /// Pratt parser / expression climber
    /// </summary>
    public class InnerExpressionPattern : Pattern
    {
        private readonly int _minPrecedence;

        public InnerExpressionPattern(int minPrecedence = 0) => _minPrecedence = minPrecedence;

        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;

            // 1. Parse the initial "nud" token (atom)
            var left = Parser.ParseAtom(ref ctx);
            if (left == null) return false;
            
            Parser.SkipTrivia(ref ctx);

            if (_minPrecedence < 10 && (left is VariableNode || left is ArrayAccessNode) && !ctx.IsAtEnd && IsAssignment(ctx.Peek().Kind))
            {
                // consume '=' or '+=' etc
                var assignToken = ctx.Consume();

                Parser.SkipTrivia(ref ctx);

                // parse the value expression
                if (!new InnerExpressionPattern(0).TryMatch(ref ctx, out var value))
                    return false;

                result = new BinaryOpNode
                {
                    Left = left as ExpressionNode,
                    Right = value as ExpressionNode,
                    Operator = assignToken.Kind
                };

                return true;
            }


            // 2. Parse operators ("led" phase)
            while (!ctx.IsAtEnd)
            {
                Parser.SkipTrivia(ref ctx);
                if (ctx.IsAtEnd) break;

                var token = ctx.Peek();
                var (precedence, isPostfix, isRightAssoc) = GetOperatorInfo(token.Kind);

                if (precedence <= _minPrecedence || precedence == 0) break;

                ctx.Consume();

                if (isPostfix)
                {
                    // Existing postfix logic... (left bracket etc)
                    if (token.Kind == TokenKind.LeftBracket)
                    {
                        // ... (omitted for brevity, keep existing)
                        Parser.SkipTrivia(ref ctx);
                        ExpressionNode? key = null;
                        if (ctx.Peek().Kind != TokenKind.RightBracket)
                        {
                            if (new InnerExpressionPattern(0).TryMatch(ref ctx, out var keyNode))
                                key = keyNode as ExpressionNode;
                        }
                        Parser.SkipTrivia(ref ctx);
                        if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.RightBracket) ctx.Consume();
                        left = new ArrayAccessNode { Array = left as ExpressionNode, Key = key };
                    }
                    else if (token.Kind == TokenKind.ObjectOperator)
                    {
                        Parser.SkipTrivia(ref ctx);
                        if (ctx.Peek().Kind == TokenKind.Identifier)
                        {
                            var prop = new IdentifierNode { Token = ctx.Consume() };
                            left = new ObjectAccessNode { Object = left as ExpressionNode, Property = prop };
                            
                            // Check if this is a method call (followed by '(')
                            Parser.SkipTrivia(ref ctx);
                            if (ctx.Peek().Kind == TokenKind.LeftParen)
                            {
                                // Parse method call arguments
                                ctx.Consume(); // consume '('
                                var args = new List<ExpressionNode>();
                                while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightParen)
                                {
                                    Parser.SkipTrivia(ref ctx);
                                    if (new InnerExpressionPattern(0).TryMatch(ref ctx, out var arg))
                                    {
                                        args.Add((ExpressionNode)arg!);
                                    }
                                    else break;
                                    Parser.SkipTrivia(ref ctx);
                                    if (ctx.Peek().Kind == TokenKind.Comma)
                                    {
                                        ctx.Consume();
                                    }
                                    else break;
                                }
                                Parser.SkipTrivia(ref ctx);
                                if (ctx.Peek().Kind == TokenKind.RightParen)
                                    ctx.Consume();
                                
                                // Create function call node from object access
                                left = new FunctionCallNode
                                {
                                    Callee = left as ExpressionNode,
                                    Args = args
                                };
                            }
                        }
                        else throw new Exception("Expected identifier after '->'");
                    }
                    else if (token.Kind == TokenKind.ScopeResolution)
                    {
                        Parser.SkipTrivia(ref ctx);
                        if (ctx.Peek().Kind == TokenKind.Identifier || ctx.Peek().Kind == TokenKind.Variable)
                        {
                            SyntaxNode? memberNode = null;
                            
                            // Check if this is a static method call (followed by '(')
                            var firstToken = ctx.Peek();
                            ctx.Consume();
                            
                            Parser.SkipTrivia(ref ctx);
                            if (ctx.Peek().Kind == TokenKind.LeftParen)
                            {
                                // Parse method call arguments
                                ctx.Consume(); // consume '('
                                var args = new List<ExpressionNode>();
                                while (!ctx.IsAtEnd && ctx.Peek().Kind != TokenKind.RightParen)
                                {
                                    Parser.SkipTrivia(ref ctx);
                                    if (new InnerExpressionPattern(0).TryMatch(ref ctx, out var arg))
                                    {
                                        args.Add((ExpressionNode)arg!);
                                    }
                                    else break;
                                    Parser.SkipTrivia(ref ctx);
                                    if (ctx.Peek().Kind == TokenKind.Comma)
                                    {
                                        ctx.Consume();
                                    }
                                    else break;
                                }
                                Parser.SkipTrivia(ref ctx);
                                if (ctx.Peek().Kind == TokenKind.RightParen)
                                    ctx.Consume();
                                
                                // Create function call node from static access
                                var member = new IdentifierNode { Token = firstToken };
                                left = new FunctionCallNode
                                {
                                    Callee = new StaticAccessNode { Target = left as ExpressionNode, MemberName = member },
                                    Args = args
                                };
                            }
                            else if (firstToken.Kind == TokenKind.Variable)
                            {
                                // Static property access with variable: Class::$prop
                                var varNode = new VariableNode { Token = firstToken };
                                left = new StaticAccessNode { Target = left as ExpressionNode, MemberName = varNode as SyntaxNode };
                            }
                            else
                            {
                                // Just static property access with identifier: Class::CONST
                                var member = new IdentifierNode { Token = firstToken };
                                left = new StaticAccessNode { Target = left as ExpressionNode, MemberName = member };
                            }
                        }
                        else throw new Exception("Expected identifier after '::'");
                    }
                    else
                    {
                        left = new PostfixExpressionNode(left, token);
                    }

                }
                else if (token.Kind == TokenKind.QuestionMark)
                {
                    // Ternary: [left] ? [trueExpr] : [falseExpr]
                    if (!new InnerExpressionPattern(0).TryMatch(ref ctx, out var trueExpr)) return false;
                    Parser.SkipTrivia(ref ctx);
                    if (ctx.Peek().Kind != TokenKind.Colon) throw new Exception("Expected ':' in ternary");
                    ctx.Consume();
                    if (!new InnerExpressionPattern(precedence - 1).TryMatch(ref ctx, out var falseExpr)) return false;
                    left = new TernaryNode { Condition = left as ExpressionNode, Then = trueExpr as ExpressionNode, Else = falseExpr as ExpressionNode };
                }
                else
                {
                    // Existing binary op logic...
                    int nextMin = isRightAssoc ? precedence - 1 : precedence;
                    Parser.SkipTrivia(ref ctx);
                    if (new InnerExpressionPattern(nextMin).TryMatch(ref ctx, out var right))
                    {
                        if (token.Kind == TokenKind.Instanceof)
                        {
                            left = new InstanceOfNode { Expression = left as ExpressionNode, ClassIdentifier = right as ExpressionNode };
                        }
                        else
                        {
                            left = new BinaryOpNode { Left = left as ExpressionNode, Right = right as ExpressionNode, Operator = token.Kind };
                        }
                    }
                    else break;
                }
            }

            result = left;
            return true;
        }

        private bool IsAssignment(TokenKind kind) => kind is
            TokenKind.AssignEquals or TokenKind.AddAssign or TokenKind.SubtractAssign or
            TokenKind.MultiplyAssign or TokenKind.DivideAssign or TokenKind.ModuloAssign or
            TokenKind.PowerAssign or TokenKind.ConcatAppend or TokenKind.NullCoalesceAssign;

        private (int Precedence, bool IsPostfix, bool IsRightAssoc) GetOperatorInfo(TokenKind kind) => kind switch
        {
            TokenKind.LogicalOrKeyword => (3, false, false),
            TokenKind.LogicalXorKeyword => (4, false, false),
            TokenKind.LogicalAndKeyword => (5, false, false),
            TokenKind.AssignEquals or TokenKind.AddAssign or TokenKind.SubtractAssign 
                or TokenKind.MultiplyAssign or TokenKind.DivideAssign or TokenKind.ModuloAssign 
                or TokenKind.PowerAssign or TokenKind.ConcatAppend or TokenKind.NullCoalesceAssign => (10, false, true),
            TokenKind.LogicalOr => (20, false, false),
            TokenKind.QuestionMark => (15, false, false),
            TokenKind.LogicalAnd => (30, false, false),
            TokenKind.LessThan or TokenKind.GreaterThan or TokenKind.LessThanOrEqual or TokenKind.GreaterThanOrEqual 
                or TokenKind.ShallowEquality or TokenKind.DeepEquality or TokenKind.ShallowInequality or TokenKind.DeepInequality 
                or TokenKind.Instanceof => (35, false, false),
            TokenKind.Add or TokenKind.Subtract or TokenKind.Concat => (40, false, false),
            TokenKind.Multiply or TokenKind.DivideBy or TokenKind.Modulo => (50, false, false),
            TokenKind.Power => (60, false, true),
            TokenKind.LeftBracket => (90, true, false),
            TokenKind.Increment or TokenKind.Decrement => (100, true, false),
            TokenKind.ObjectOperator or TokenKind.ScopeResolution => (110, true, false),
            _ => (0, false, false)
        };
    }


    /// <summary>
    /// Prefix expression (+$a, ++$x)
    /// </summary>
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
            Operand?.ToJson(in span, in tokens, builder);
            builder.Append("}");
        }
    }

    /// <summary>
    /// Postfix expression ($i++, $i--)
    /// </summary>
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
            Operand?.ToJson(in span, in tokens, builder);
            builder.Append("}");
        }
    }

    /// <summary>
    /// Helper collection for grammar expressions
    /// </summary>
    public partial class ExpressionCollection
    {
        public InnerExpressionPattern Inner(int minPrecedence = 0) => new InnerExpressionPattern(minPrecedence);
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static ExpressionCollection Expressions = new ExpressionCollection();
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
