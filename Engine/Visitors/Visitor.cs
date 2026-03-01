using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public class Visitor : IVisitor
{
    private readonly IVisitor[] _visitors;

    public Visitor(params IVisitor[] visitors)
    {
        _visitors = visitors;
    }

    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        foreach (var visitor in _visitors)
            node.Accept(visitor, in span);
    }
}