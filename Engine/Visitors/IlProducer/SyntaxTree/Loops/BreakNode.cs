using System.Reflection.Emit;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Visitor implementation for <see cref="BreakNode"/> — resolves the target 
/// loop nesting level and emits a jump to the corresponding loop's exit label.
///
/// <para>
/// In PHP, <c>break</c> accepts an optional numeric argument (e.g., <c>break 2;</c>) 
/// that determines how many nested enclosing structures are being broken out of. 
/// This is managed via the <see cref="IlProducer"/>'s loop stack, which tracks 
/// <see cref="LoopContext"/> frames for all active <c>while</c>, <c>for</c>, 
/// and <c>foreach</c> loops.
/// </para>
/// </summary>
public partial class BreakNode : ExpressionNode
{
    /// <summary>
    /// Accepts a visitor to emit Intermediate Language (IL) for the <c>break</c> statement.
    /// </summary>
    /// <param name="visitor">
    /// The visitor processing this node. Only <see cref="IlProducer"/> generates IL; 
    /// other visitors will perform a no-op as this node contains no child expressions.
    /// </param>
    /// <param name="source">
    /// The source code span used to parse the numeric value of the <see cref="Label"/> token.
    /// </param>
    /// <remarks>
    /// <para>
    /// The IL emission process follows these steps:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// Parses the <see cref="Label"/> token to determine the target depth. If the token is 
    /// missing or invalid, it defaults to <c>1</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Retrieves the <see cref="LoopContext"/> from the producer's stack at the specified depth.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Emits <see cref="OpCodes.Leave"/> targeting the loop's <c>BreakLabel</c>. 
    /// <c>Leave</c> is used instead of <c>Br</c> to ensure the evaluation stack is 
    /// emptied and any <c>finally</c> blocks in the IL protected regions are executed.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        // This node only has an effect during IL generation.
        if (visitor is not IlProducer ilProducer) return;

        // 1. Determine the jump depth. PHP levels are 1-based.
        int levels = 1;
        if (Label.HasValue)
        {
            var text = Label.Value.TextValue(source);
            // If the label isn't a valid integer, we fall back to 1.
            // In a more robust compiler, this would be a semantic error.
            if (!int.TryParse(text, out levels))
                levels = 1;
        }

        // 2. Locate the loop exit label in the control flow stack.
        // The stack is populated by WhileNode, ForNode, etc., before they visit their bodies.
        var target = ilProducer.GetLoopAtLevel(levels);

        if (target != null)
        {
            // 3. Emit the jump to the loop's exit point.
            // OpCodes.Leave clears the evaluation stack, preventing "stack imbalance"
            // exceptions if the break occurs mid-expression.
            ilProducer.GetILGenerator().Emit(OpCodes.Leave, target.BreakLabel);
        }

        // 4. Signal that the stack is now empty at this point in the IL.
        // Since break terminates the current execution path, it leaves nothing on the stack.
        ilProducer.LastEmittedType = null;
    }
}