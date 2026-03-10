using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    private Stack<Label> _continueLabels = new();

    public void VisitContinueNode(ContinueNode node, in ReadOnlySpan<char> source)
    {
        int level = 1;
        if (node.Label.HasValue)
            level = int.Parse(node.Label.Value.TextValue(source));

        var labels = _continueLabels.ToArray();
        if (level > labels.Length)
            throw new Exception($"Cannot continue {level} levels, only {labels.Length} loop(s) deep.");

        Emit(OpCodes.Br, labels[level - 1]);
    }
}