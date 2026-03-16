using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for an <c>if</c> statement, including any <c>elseif</c> and <c>else</c> branches.
    /// </summary>
    /// <param name="node">The <see cref="IfNode"/> representing the full if/elseif/else construct.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    /// <remarks>
    /// <para>
    /// Two labels are defined up front: <c>falseLabel</c>, branched to when the condition is
    /// <see langword="false"/>, and <c>exitLabel</c>, branched to after the true body completes.
    /// <c>exitLabel</c> is pushed onto <c>_exitLabels</c> for the duration of the node so that
    /// nested <c>break</c> statements can target it.
    /// </para>
    /// <para>
    /// If the condition expression is a <see cref="VariableNode"/> or <see cref="FunctionCallNode"/>,
    /// the boxed value is unboxed to <see cref="bool"/> before the branch, since these may return
    /// boxed booleans (e.g. from <c>isset()</c>).
    /// </para>
    /// <para>
    /// After emitting the true body, the statements are scanned for a <see cref="BreakNode"/>. If
    /// one is found it is emitted directly and the unconditional branch to <c>exitLabel</c> is
    /// suppressed, since control flow has already been transferred. Otherwise a
    /// <see cref="OpCodes.Br"/> to <c>exitLabel</c> is emitted to skip the false branches.
    /// </para>
    /// <para>
    /// <c>falseLabel</c> is then marked, followed by any <c>elseif</c> branches and the optional
    /// <c>else</c> body. Finally, <c>exitLabel</c> is marked and popped from <c>_exitLabels</c>.
    /// </para>
    /// </remarks>
    public void VisitIfNode(IfNode node, in ReadOnlySpan<char> source)
    {
        var exitLabel = DefineLabel();
        var falseLabel = DefineLabel();

        _exitLabels.Push(exitLabel);

        if (node.Expression != null)
        {
            node.Expression.Accept(this, source);
            // Unbox any boxed value to bool for proper condition evaluation
            // This handles variables and function calls like isset() that return boxed bools  
            if (node.Expression is VariableNode or FunctionCallNode)
                Emit(OpCodes.Unbox_Any, typeof(bool));
            Emit(OpCodes.Brfalse, falseLabel);
        }

        if (node.Body != null)
            node.Body.Accept(this, source);

        bool bodyExits = false;
        if (node.Body != null && node.Body.Statements.Count > 0)
        {
            for (int i = 0; i < node.Body.Statements.Count; i++)
            {
                if (node.Body.Statements[i] is BreakNode breakNode)
                {
                    breakNode.Accept(this, source);
                    bodyExits = true;
                    break;
                }
            }
        }

        if (!bodyExits)
            Emit(OpCodes.Br, exitLabel);

        MarkLabel(falseLabel);

        foreach (var elseIf in node.ElseIfs)
            elseIf.Accept(this, source);

        if (node.ElseNode != null)
            node.ElseNode.Accept(this, source);

        MarkLabel(exitLabel);

        _exitLabels.Pop();
    }
}