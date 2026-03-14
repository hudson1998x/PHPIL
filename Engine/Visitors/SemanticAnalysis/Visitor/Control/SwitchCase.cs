using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis.Context;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitSwitchNode(SwitchNode node, in ReadOnlySpan<char> source)
    {
        _currentContext.Push(new StackFrame { CanAscend = true });

        if (node.Expression != null)
            node.Expression.Accept(this, source);

        foreach (var caseNode in node.Cases)
            caseNode.Accept(this, source);

        if (node.Default != null)
            node.Default.Accept(this, source);

        _currentContext.Pop();
    }

    public void VisitCaseNode(CaseNode node, in ReadOnlySpan<char> source)
    {
        _currentContext.Push(new StackFrame { CanAscend = true });

        if (node.Expression != null)
            node.Expression.Accept(this, source);

        if (node.Body != null)
            node.Body.Accept(this, source);

        _currentContext.Pop();
    }
}
