using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class BinaryOpNode : ExpressionNode
    {
        public SyntaxNode? Left;
        public SyntaxNode? Right;

        public TokenKind Operator;
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source);
    }
}