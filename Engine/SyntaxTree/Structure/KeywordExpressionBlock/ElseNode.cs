using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class ElseNode : SyntaxNode
    {
        public BlockNode? Body { get; set; }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitElseNode(ElseNode node, in ReadOnlySpan<char> source);
    }
}