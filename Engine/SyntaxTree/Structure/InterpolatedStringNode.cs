using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure
{
    public partial class InterpolatedStringNode : ExpressionNode
    {
        public List<ExpressionNode> Parts { get; set; } = new();

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitInterpolatedStringNode(this, source);
        }

        public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
        {
            builder.Append("{\"type\":\"InterpolatedStringNode\",\"parts\":[");
            for (int i = 0; i < Parts.Count; i++)
            {
                Parts[i].ToJson(in span, in tokens, builder);
                if (i < Parts.Count - 1) builder.Append(",");
            }
            builder.Append("]}");
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    using PHPIL.Engine.SyntaxTree.Structure;
    public partial interface IVisitor
    {
        void VisitInterpolatedStringNode(InterpolatedStringNode node, in ReadOnlySpan<char> source);
    }
}

