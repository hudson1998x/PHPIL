using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class IfNode : SyntaxNode
    {
        public ExpressionNode? Expression;
    
        public BlockNode? Body;
    
        public List<ElseIfNode> ElseIfs = [];
    
        public SyntaxNode? ElseNode;
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitIfNode(IfNode node, in ReadOnlySpan<char> source);
    }
}