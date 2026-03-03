using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class VariableDeclaration : ExpressionNode
    {
        public Token VariableName;
    
        public ExpressionNode? VariableValue { get; set; }

        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            visitor.VisitVariableDeclaration(this, source);
        }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source);
    }
}