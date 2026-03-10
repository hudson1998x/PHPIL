using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class ContinueNode : ExpressionNode
    {
        public Token? Label { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
            => visitor.VisitContinueNode(this, source);
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitContinueNode(ContinueNode node, in ReadOnlySpan<char> source);
    }
}