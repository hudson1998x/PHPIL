using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class SwitchNode : SyntaxNode
    {
        public ExpressionNode? Expression;
        public List<CaseNode> Cases = [];
        public BlockNode? Default;

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitSwitchNode(this, source);
        }
    }

    public partial class CaseNode : SyntaxNode
    {
        public ExpressionNode? Expression;
        public BlockNode? Body;

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitCaseNode(this, source);
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitSwitchNode(SwitchNode node, in ReadOnlySpan<char> source);
        void VisitCaseNode(CaseNode node, in ReadOnlySpan<char> source);
    }
}
