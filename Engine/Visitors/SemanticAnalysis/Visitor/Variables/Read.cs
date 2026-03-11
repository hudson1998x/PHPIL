using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis.Context;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source)
    {
        var name = node.Token.TextValue(in source);

        VariableInfo? info = null;

        foreach (var frame in _currentContext.Reverse())
        {
            if (frame.Variables.TryGetValue(name, out info))
            {
                if (_currentContext.Count > 1)
                    info.IsCaptured = true;

                info.IsUsed = true;
                node.AnalysedType = info.Type;

                if (info.Node != null)
                    node.AnalysedType = info.Node.AnalysedType;

                return;
            }

            if (!frame.CanAscend) break;
        }

        if (_currentContext.Count > 0)
        {
            var currentFrame = _currentContext.Peek();
            var newInfo = new VariableInfo(node.AnalysedType, null)
            {
                IsUsed = true,
                IsCaptured = _currentContext.Count > 1
            };
            currentFrame.Variables[name] = newInfo;
        }
    }
}