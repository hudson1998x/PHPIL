using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class IfNode : SyntaxNode
    {
        public ExpressionNode? Expression;
    
        public BlockNode? Body;
    
        public List<ElseIfNode> ElseIfs = [];
    
        public SyntaxNode? ElseNode;

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitIfNode(this, source);
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitIfNode(IfNode node, in ReadOnlySpan<char> source);
    }
}