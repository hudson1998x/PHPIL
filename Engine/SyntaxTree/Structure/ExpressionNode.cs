using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class ExpressionNode : SyntaxNode
    {
        public List<SyntaxNode> Statements = [];

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitExpressionNode(this, source);
        }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitExpressionNode(ExpressionNode node, in ReadOnlySpan<char> source);
        void VisitArrayAccessNode(ArrayAccessNode node, in ReadOnlySpan<char> source);
    }
}