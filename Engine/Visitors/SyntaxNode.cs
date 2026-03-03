using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree;

public partial class SyntaxNode
{
    public void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        visitor.Visit(this, in source);
    }
}