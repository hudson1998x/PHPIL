using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Productions.Patterns
{
    public class ArrayLiteralPattern : Pattern
    {
        public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
        {
            result = null;
            int start = ctx.Save();

            Parser.SkipTrivia(ref ctx);
            if (ctx.IsAtEnd) return false;

            Token token = ctx.Peek();
            bool isShort = token.Kind == TokenKind.LeftBracket;
            bool isFunc = token.Kind == TokenKind.Array ||
                          (token.Kind == TokenKind.Identifier && token.TextValue(in ctx.Source) == "array");

            if (!isShort && !isFunc)
            {
                ctx.Restore(start);
                return false;
            }

            // Consume opening
            if (isShort)
            {
                ctx.Consume();
            }
            else
            {
                ctx.Consume(); // 'array'
                Parser.SkipTrivia(ref ctx);
                if (ctx.IsAtEnd || ctx.Peek().Kind != TokenKind.LeftParen)
                {
                    ctx.Restore(start);
                    return false;
                }
                ctx.Consume(); // '('
            }

            var items = new List<ArrayItemNode>();

            while (!ctx.IsAtEnd)
            {
                Parser.SkipTrivia(ref ctx);
                if (ctx.IsAtEnd) break;

                Token nextToken = ctx.Peek();
                if ((isShort && nextToken.Kind == TokenKind.RightBracket) ||
                    (!isShort && nextToken.Kind == TokenKind.RightParen))
                {
                    break;
                }

                int itemStart = ctx.Position;
                if (new InnerExpressionPattern(0).TryMatch(ref ctx, out var firstExpr))
                {
                    ExpressionNode? key = null;
                    ExpressionNode value;

                    Parser.SkipTrivia(ref ctx);

                    if (!ctx.IsAtEnd && ctx.Peek().Kind == TokenKind.Arrow)
                    {
                        ctx.Consume(); // Consume '=>'
                        key = (ExpressionNode)firstExpr!;
                        Parser.SkipTrivia(ref ctx);

                        if (new InnerExpressionPattern(0).TryMatch(ref ctx, out var valExpr))
                        {
                            value = (ExpressionNode)valExpr!;
                        }
                        else
                        {
                            ctx.Restore(start);
                            return false;
                        }
                    }
                    else
                    {
                        value = (ExpressionNode)firstExpr!;
                    }

                    items.Add(new ArrayItemNode
                    {
                        Key = key,
                        Value = value,
                        RangeStart = itemStart,
                        RangeEnd = ctx.Position
                    });
                }
                else
                {
                    ctx.Restore(start);
                    return false;
                }

                Parser.SkipTrivia(ref ctx);
                if (ctx.IsAtEnd) break;

                if (ctx.Peek().Kind == TokenKind.Comma)
                {
                    ctx.Consume();
                }
                else
                {
                    Parser.SkipTrivia(ref ctx);
                    if (ctx.IsAtEnd) break;

                    Token afterItem = ctx.Peek();
                    bool isClosing = (isShort && afterItem.Kind == TokenKind.RightBracket) ||
                                     (!isShort && afterItem.Kind == TokenKind.RightParen);

                    if (!isClosing)
                    {
                        ctx.Restore(start);
                        return false;
                    }
                    break;
                }
            }

            Parser.SkipTrivia(ref ctx);
            if (ctx.IsAtEnd)
            {
                ctx.Restore(start);
                return false;
            }

            Token closing = ctx.Peek();
            if ((isShort && closing.Kind != TokenKind.RightBracket) ||
                (!isShort && closing.Kind != TokenKind.RightParen))
            {
                ctx.Restore(start);
                return false;
            }

            ctx.Consume(); // consume closing

            result = new ArrayLiteralNode
            {
                Items = items,
                RangeStart = start,
                RangeEnd = ctx.Position
            };

            return true;
        }
    }

    public class ArrayItemNode : SyntaxNode
    {
        public ExpressionNode? Key { get; set; }
        public ExpressionNode Value { get; set; } = null!;

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            Key?.Accept(visitor, source);
            Value.Accept(visitor, source);
        }

        public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
        {
            builder.Append("{");
            if (Key != null)
            {
                builder.Append("\"key\":");
                Key.ToJson(in span, in tokens, builder);
                builder.Append(",");
            }
            builder.Append("\"value\":");
            Value.ToJson(in span, in tokens, builder);
            builder.Append("}");
        }
    }

    public class ArrayLiteralNode : ExpressionNode
    {
        public List<ArrayItemNode> Items { get; set; } = new List<ArrayItemNode>();

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
            => visitor.VisitArrayLiteralNode(this, source);

        public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
        {
            builder.Append("{\"type\":\"ArrayLiteralNode\",\"items\":[");
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].ToJson(in span, in tokens, builder);
                if (i < Items.Count - 1) builder.Append(",");
            }
            builder.Append("]}");
        }
    }
}

namespace PHPIL.Engine.Productions
{
    public static partial class Grammar
    {
        public static ArrayLiteralPattern ArrayLiteral() => new ArrayLiteralPattern();
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitArrayLiteralNode(ArrayLiteralNode node, in ReadOnlySpan<char> source);
    }
}