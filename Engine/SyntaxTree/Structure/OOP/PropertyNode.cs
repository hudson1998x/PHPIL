using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class PropertyNode : SyntaxNode
    {
        public PhpModifiers Modifiers { get; set; }
        public IdentifierNode Name { get; set; }
        public SyntaxNode? DefaultValue { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitPropertyNode(this, in source);
        }
    }
}
