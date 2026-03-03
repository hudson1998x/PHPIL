using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class VariableNode : ExpressionNode
    {
        public Token Token { get; set; }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source);
    }
}