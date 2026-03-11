using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis.Context;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitIfNode(IfNode node, in ReadOnlySpan<char> source)
    {
        if (node.Expression != null)
            node.Expression.Accept(this, source);

        if (node.Body != null)
        {
            _currentContext.Push(new StackFrame { CanAscend = true });
            node.Body.Accept(this, source);
            _currentContext.Pop();
        }

        foreach (var elseIf in node.ElseIfs)
            elseIf.Accept(this, source);

        if (node.ElseNode != null)
            node.ElseNode.Accept(this, source);
    }
}