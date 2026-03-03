using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public class ElseIfNode : IfNode
    {
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitElseIfNode(this, source);
        }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitElseIfNode(ElseIfNode node, in ReadOnlySpan<char> source);
    }
}