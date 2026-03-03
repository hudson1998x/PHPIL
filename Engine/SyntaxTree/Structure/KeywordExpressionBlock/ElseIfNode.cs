using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public class ElseIfNode : IfNode
    {
    
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitElseIfNode(FunctionCallNode node, in ReadOnlySpan<char> source);
    }
}