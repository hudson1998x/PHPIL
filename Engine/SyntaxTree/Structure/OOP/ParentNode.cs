using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class ParentNode : ExpressionNode
    {
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitParentNode(this, in source);
        }
    }
}
