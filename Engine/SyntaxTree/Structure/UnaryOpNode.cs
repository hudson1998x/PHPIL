using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class UnaryOpNode : ExpressionNode
    {
        public TokenKind Operator;
    
        public SyntaxNode? Operand;

        public bool Prefix = false;
        
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitUnaryOpNode(this, source);
        }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source);
    }
}