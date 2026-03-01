using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree;

public partial class GroupNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        if (Inner is not null)
        {
            visitor.Visit(Inner, in source);   
        }
    }
}