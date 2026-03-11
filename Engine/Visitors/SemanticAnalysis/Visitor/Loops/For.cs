using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis.Context;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitForNode(For node, in ReadOnlySpan<char> source)
    {
        _currentContext.Push(new StackFrame { CanAscend = true });

        if (node.Init != null)
            node.Init.Accept(this, source);

        if (node.Init != null)
            node.Init.Accept(this, source);

        if (node.Condition != null)
            node.Condition.Accept(this, source);

        if (node.Increment != null)
            node.Increment.Accept(this, source);

        if (node.Body != null)
            node.Body.Accept(this, source);

        _currentContext.Pop();
    }
}