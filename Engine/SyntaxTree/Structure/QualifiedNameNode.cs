using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure
{
    public partial class QualifiedNameNode : ExpressionNode
    {
        public List<Token> Parts { get; init; } = [];
        public bool IsFullyQualified { get; init; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitQualifiedNameNode(this, source);
        }

        public override void ToJson(in ReadOnlySpan<char> span, in ReadOnlySpan<Token> tokens, StringBuilder builder)
        {
            builder.Append("{\"type\":\"QualifiedNameNode\",\"parts\":[");
            for (int i = 0; i < Parts.Count; i++)
            {
                if (i > 0) builder.Append(",");
                Parts[i].ToJson(in span, builder);
            }
            builder.Append($"],\"isFullyQualified\":{(IsFullyQualified ? "true" : "false")}}}");
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    using PHPIL.Engine.SyntaxTree.Structure;
    public partial interface IVisitor
    {
        void VisitQualifiedNameNode(QualifiedNameNode node, in ReadOnlySpan<char> source);
    }
}
