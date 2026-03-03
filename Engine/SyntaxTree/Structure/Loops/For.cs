using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class For : ExpressionNode
    {
        // These must be properties in your For class
        public SyntaxNode? Init { get; set; }
        public SyntaxNode? Condition { get; set; }
        public SyntaxNode? Increment { get; set; }
        public SyntaxNode? Body { get; set; }
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitForNode(For node, in ReadOnlySpan<char> source);
    }
}