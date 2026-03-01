using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial interface IVisitor
{
    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span);
}