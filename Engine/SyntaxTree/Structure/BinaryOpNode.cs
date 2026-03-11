using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class BinaryOpNode : ExpressionNode
    {
        public SyntaxNode? Left;
        public SyntaxNode? Right;

        public TokenKind Operator;

        public bool NeedsValue;

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitBinaryOpNode(this, source);
        }
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source);
    }
}