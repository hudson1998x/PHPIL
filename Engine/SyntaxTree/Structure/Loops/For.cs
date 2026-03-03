using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class For : ExpressionNode
    {
        // These must be properties in your For class
        public SyntaxNode? Init { get; set; }
        public SyntaxNode? Condition { get; set; }
        public SyntaxNode? Increment { get; set; }
        public SyntaxNode? Body { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitForNode(this, source);
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitForNode(For node, in ReadOnlySpan<char> source);
    }
}