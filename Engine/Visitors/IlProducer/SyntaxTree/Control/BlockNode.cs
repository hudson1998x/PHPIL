using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

public partial class BlockNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        if (visitor is IlProducer ilProducer)
        {
            ilProducer.EnterScope();
        }

        foreach (var syntaxNode in Statements)
        {
            visitor.Visit(syntaxNode, in source);
        }

        if (visitor is IlProducer scope)
        {
            scope.ExitScope();   
        }
    }
}