using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class VariableDeclaration : ExpressionNode
    {
        public Token VariableName;
    
        public ExpressionNode? VariableValue { get; set; }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source);
    }
}