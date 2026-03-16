using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Stack of exit labels for the currently active breakable constructs (loops and
    /// <c>switch</c> statements), maintained in innermost-first order.
    /// </summary>
    private Stack<Label> _breakLabels = new();

    /// <summary>
    /// Emits IL for a <c>break</c> statement, branching to the exit label of the target
    /// enclosing loop or <c>switch</c>.
    /// </summary>
    /// <param name="node">The <see cref="BreakNode"/> representing the break statement.</param>
    /// <param name="source">The original source text, used to parse the optional numeric level.</param>
    /// <exception cref="Exception">
    /// Thrown when the requested break level exceeds the number of currently active breakable
    /// constructs.
    /// </exception>
    /// <remarks>
    /// PHP allows <c>break N</c> to exit <c>N</c> levels of nesting. The level defaults to
    /// <c>1</c> when omitted. <c>_breakLabels</c> is snapshotted to an array and indexed at
    /// <c>level - 1</c> to find the target label, with index <c>0</c> corresponding to the
    /// innermost construct.
    /// </remarks>
    public void VisitBreakNode(BreakNode node, in ReadOnlySpan<char> source)
    {
        int level = 1;
        if (node.Label.HasValue)
            level = int.Parse(node.Label.Value.TextValue(source));

        var labels = _breakLabels.ToArray();
        if (level > labels.Length)
            throw new Exception($"Cannot break {level} levels, only {labels.Length} loop(s) deep.");

        Emit(OpCodes.Br, labels[level - 1]);
    }
}