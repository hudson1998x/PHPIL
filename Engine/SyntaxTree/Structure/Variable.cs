using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class VariableNode : ExpressionNode
    {
        public Token Token { get; set; }
        
        
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitVariableNode(this, source);
        }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source);
    }
}