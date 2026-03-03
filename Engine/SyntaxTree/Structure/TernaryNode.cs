using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class TernaryNode : ExpressionNode
    {
        public ExpressionNode? Condition { get; init; }
        public ExpressionNode? Then      { get; init; }
        public ExpressionNode? Else      { get; init; }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source);
    }
}