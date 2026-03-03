using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class ElseNode : SyntaxNode
    {
        public BlockNode? Body { get; set; }
        
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitElseNode(this, source);
        }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitElseNode(ElseNode node, in ReadOnlySpan<char> source);
    }
}