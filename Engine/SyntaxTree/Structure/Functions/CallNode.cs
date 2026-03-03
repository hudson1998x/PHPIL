using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class FunctionCallNode : ExpressionNode
    {
        public ExpressionNode? Callee { get; init; }  // what's being called
        public List<ExpressionNode> Args { get; init; } = [];

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitFunctionCallNode(this, source);
        }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source);
    }
}