using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis.Context;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
        if (node.VariableValue is not null)
        {
            node.VariableValue.Accept(this, source);

            if (node.VariableValue is VariableDeclaration nested)
            {
                nested.EmitValue = true;
            }
        }

        node.AnalysedType = node.VariableValue?.AnalysedType ?? AnalysedType.Mixed;

        ResolveOrDeclareVariable(node.VariableName.TextValue(in source), node.AnalysedType, node);
    }
    
    private void ResolveOrDeclareVariable(
        string name,
        AnalysedType type,
        VariableDeclaration? node = null,
        bool fromNestedScope = false,
        bool markUsed = false)
    {
        foreach (var frame in _currentContext.Reverse())
        {
            if (frame.Variables.TryGetValue(name, out var info))
            {
                info.Type = type;
                if (node != null) info.Node ??= node;
                if (fromNestedScope) info.IsCaptured = true;
                if (markUsed) info.IsUsed = true;
                return;
            }

            if (!frame.CanAscend) break;
        }

        if (_currentContext.Count > 0)
        {
            var currentFrame = _currentContext.Peek();
            currentFrame.Variables[name] = new VariableInfo(type, node!);
            if (fromNestedScope) currentFrame.Variables[name].IsCaptured = true;
            if (markUsed) currentFrame.Variables[name].IsUsed = true;
        }
        else
        {
            var globalFrame = new StackFrame();
            globalFrame.Variables[name] = new VariableInfo(type, node!);
            if (fromNestedScope) globalFrame.Variables[name].IsCaptured = true;
            if (markUsed) globalFrame.Variables[name].IsUsed = true;
            _currentContext.Push(globalFrame);
        }
    }
}