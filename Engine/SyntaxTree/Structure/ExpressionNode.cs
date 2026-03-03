using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class ExpressionNode : SyntaxNode
    {
        public List<SyntaxNode> Statements = [];
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitExpressionNode(ExpressionNode node, in ReadOnlySpan<char> source);
    }
}