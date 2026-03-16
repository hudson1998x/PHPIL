using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a <c>for</c> loop.
    /// </summary>
    /// <param name="node">The <see cref="For"/> node representing the for loop.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    /// <remarks>
    /// <para>
    /// Three labels are defined: <c>conditionLabel</c> (loop header), <c>incrementLabel</c>
    /// (increment step, targeted by <c>continue</c>), and <c>exitLabel</c> (post-loop, targeted
    /// by <c>break</c>). Both are pushed onto their respective label stacks for the duration of
    /// the loop body.
    /// </para>
    /// <para>
    /// The emitted loop structure is:
    /// </para>
    /// <list type="number">
    ///   <item><description>Emit the initialiser expression, if present.</description></item>
    ///   <item><description>Mark <c>conditionLabel</c>; evaluate the condition expression — if it is a <see cref="VariableNode"/> or <see cref="FunctionCallNode"/>, unbox to <see cref="int"/> before branching to <c>exitLabel</c> on <see langword="false"/>.</description></item>
    ///   <item><description>Emit the loop body.</description></item>
    ///   <item><description>Mark <c>incrementLabel</c>; emit the increment expression and pop its result from the stack.</description></item>
    ///   <item><description>Branch unconditionally back to <c>conditionLabel</c>.</description></item>
    ///   <item><description>Mark <c>exitLabel</c>; pop both labels from their stacks.</description></item>
    /// </list>
    /// </remarks>
    public void VisitForNode(For node, in ReadOnlySpan<char> source)
    {
        var conditionLabel = DefineLabel();
        var incrementLabel = DefineLabel();
        var exitLabel = DefineLabel();

        _breakLabels.Push(exitLabel);
        _continueLabels.Push(incrementLabel);

        if (node.Init != null)
            node.Init.Accept(this, source);

        MarkLabel(conditionLabel);

        if (node.Condition != null)
        {
            node.Condition.Accept(this, source);
            // Unbox any boxed value to int for proper condition evaluation
            // This handles variables and function calls like isset() that return boxed bools
            if (node.Condition is VariableNode or FunctionCallNode)
                Emit(OpCodes.Unbox_Any, typeof(int));
            Emit(OpCodes.Brfalse, exitLabel);
        }

        if (node.Body != null)
            node.Body.Accept(this, source);

        MarkLabel(incrementLabel);

        if (node.Increment != null)
        {
            node.Increment.Accept(this, source);
            Emit(OpCodes.Pop);
        }

        Emit(OpCodes.Br, conditionLabel);

        MarkLabel(exitLabel);

        _breakLabels.Pop();
        _continueLabels.Pop();
    }
}