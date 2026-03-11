
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class ObjectAccessNode : ExpressionNode
    {
        public ExpressionNode Object { get; set; } = null!;
        public IdentifierNode Property { get; set; } = null!;

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> span)
        {
            visitor.VisitObjectAccessNode(this, in span);
        }
    }
}
