using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure
{
    public partial class NamespaceNode : SyntaxNode
    {
        public QualifiedNameNode? Name { get; init; }
        public List<SyntaxNode> Statements { get; init; } = [];

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitNamespaceNode(this, source);
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    using PHPIL.Engine.SyntaxTree.Structure;
    public partial interface IVisitor
    {
        void VisitNamespaceNode(NamespaceNode node, in ReadOnlySpan<char> source);
    }
}
