using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class TraitNode : SyntaxNode
    {
        public QualifiedNameNode Name { get; set; }
        public List<SyntaxNode> Members { get; set; } = new();

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitTraitNode(this, in source);
        }
    }
}
