using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class BlockNode : SyntaxNode
    {
        public List<SyntaxNode> Statements = [];

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitBlockNode(this, source);
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitBlockNode(BlockNode node, in ReadOnlySpan<char> source);
    }
}