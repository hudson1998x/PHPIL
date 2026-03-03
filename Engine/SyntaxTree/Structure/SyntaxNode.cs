using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class SyntaxNode
    {
        public int RangeStart;
    
        public int RangeEnd;
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitSyntaxNode(SyntaxNode node, in ReadOnlySpan<char> source);
    }
}