using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public class ReturnNode : ExpressionNode
    {
        public ExpressionNode? Expression;
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitReturnNode(ReturnNode node, in ReadOnlySpan<char> source);
    }
}