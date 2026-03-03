using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class GroupNode : ExpressionNode
    {
        public ExpressionNode? Inner { get; init; }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitGroupNode(GroupNode node, in ReadOnlySpan<char> source);
    }
}