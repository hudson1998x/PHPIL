using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class UnaryOpNode : ExpressionNode
    {
        public TokenKind Operator;
    
        public SyntaxNode? Operand;

        public bool Prefix = false;
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source);
    }
}