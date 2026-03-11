using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis.Context;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitFunctionNode(FunctionNode node, in ReadOnlySpan<char> source)
    {
        var functionFrame = new StackFrame
        {
            CanAscend = false // Functions in PHP don't capture outer scope by default unless using 'use'
        };

        _currentContext.Push(functionFrame);

        foreach (var param in node.Params)
        {
            VisitFunctionParameter(param, source);
        }

        if (node.Body != null)
        {
            node.Body.Accept(this, source);
        }

        _currentContext.Pop();
    }

    public void VisitFunctionParameter(FunctionParameter node, in ReadOnlySpan<char> source)
    {
        var paramName = node.Name.TextValue(in source);
        // Parameters default to mixed type if no type hint is provided
        // In this simple implementation we just consider them initialized mixed variables
        ResolveOrDeclareVariable(paramName, AnalysedType.Mixed);
    }
}
