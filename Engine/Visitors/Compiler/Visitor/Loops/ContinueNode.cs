using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Stack of continue labels for the currently active loops, maintained in innermost-first order.
    /// </summary>
    private Stack<Label> _continueLabels = new();

    /// <summary>
    /// Emits IL for a <c>continue</c> statement, branching to the continue label of the target
    /// enclosing loop.
    /// </summary>
    /// <param name="node">The <see cref="ContinueNode"/> representing the continue statement.</param>
    /// <param name="source">The original source text, used to parse the optional numeric level.</param>
    /// <exception cref="Exception">
    /// Thrown when the requested continue level exceeds the number of currently active loops.
    /// </exception>
    /// <remarks>
    /// Mirrors <see cref="VisitBreakNode"/> in structure and behaviour, but targets
    /// <c>_continueLabels</c> rather than <c>_breakLabels</c>. PHP's <c>continue N</c> skips
    /// to the next iteration of the <c>N</c>th enclosing loop, defaulting to <c>1</c> when
    /// omitted. Index <c>0</c> in the snapshotted label array corresponds to the innermost loop.
    /// </remarks>
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