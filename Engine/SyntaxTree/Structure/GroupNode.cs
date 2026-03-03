using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class GroupNode : ExpressionNode
    {
        public ExpressionNode? Inner { get; init; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitGroupNode(this, source);
        }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitGroupNode(GroupNode node, in ReadOnlySpan<char> source);
    }
}