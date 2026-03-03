using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree.Structure.Loops;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.Loops
{
    public class ForeachNode : SyntaxNode
    {
        public ExpressionNode? Iterable { get; set; }
        public VariableNode? Key { get; set; }
        public VariableNode? Value { get; set; }
        public BlockNode? Body { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitForeachNode(this, source);
        }

        public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
        {
            builder.Append('{');
            builder.Append("\"type\": \"foreach\"");
            builder.Append(",\"iterable\": ");
            if (Iterable != null)
            {
                Iterable.ToJson(in span, in tokens, builder);
            }
            else
            {
                builder.Append("null");
            }
            
            builder.Append(",\"key\": ");
            
            if (Key != null)
            {
                Key.ToJson(in span, in tokens, builder);
            }
            else
            {
                builder.Append("null");
            }
            
            builder.Append(",\"value\": ");
            
            if (Value != null)
            {
                Value.ToJson(in span, in tokens, builder);
            }
            else
            {
                builder.Append("null");
            }
            
            builder.Append(",\"body\": ");
            
            if (Body != null)
            {
                Body.ToJson(in span, in tokens, builder);
            }
            else
            {
                builder.Append("null");
            }

            builder.Append('}');
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        public void VisitForeachNode(ForeachNode node, in ReadOnlySpan<char> source);
    }
}