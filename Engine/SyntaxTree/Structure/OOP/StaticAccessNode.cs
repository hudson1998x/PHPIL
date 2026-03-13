using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree.Structure.OOP
{
    public class StaticAccessNode : ExpressionNode
    {
        public ExpressionNode Target { get; set; }
        public SyntaxNode? MemberName { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitStaticAccessNode(this, in source);
        }
    }
}
