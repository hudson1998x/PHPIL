using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class TernaryNode : ExpressionNode
    {
        public ExpressionNode? Condition { get; init; }
        public ExpressionNode? Then      { get; init; }
        public ExpressionNode? Else      { get; init; }
        
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitTernaryNode(this, source);
        }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source);
    }
}