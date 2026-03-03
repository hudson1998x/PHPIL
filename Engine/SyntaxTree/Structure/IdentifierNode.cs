using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class IdentifierNode : SyntaxNode
    {
        public Token Token;
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitIdentifierNode(IdentifierNode node, in ReadOnlySpan<char> source);
    }
}