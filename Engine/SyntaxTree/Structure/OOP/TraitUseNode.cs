using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class TraitUseNode : SyntaxNode
    {
        public List<SyntaxNode> Traits { get; set; } = new();

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitTraitUseNode(this, in source);
        }
    }
}
