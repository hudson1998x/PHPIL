using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class FunctionCallNode : ExpressionNode
    {
        public ExpressionNode? Callee { get; init; }  // what's being called
        public List<ExpressionNode> Args { get; init; } = [];
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source);
    }
}