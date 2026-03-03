using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class BlockNode : SyntaxNode
    {
        public List<SyntaxNode> Statements = [];
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitBlockNode(BlockNode node, in ReadOnlySpan<char> source);
    }
}