using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

public partial class ExpressionNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        foreach (var syntaxNode in Statements)
        {
            visitor.Visit(syntaxNode, in source);
        }
    }
}