using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a <c>switch</c> statement, including all <c>case</c> and optional <c>default</c> branches.
    /// </summary>
    /// <param name="node">The <see cref="SwitchNode"/> representing the full switch construct.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    /// <remarks>
    /// <para>
    /// An <c>exitLabel</c> is defined and pushed onto <c>_breakLabels</c> so that <c>break</c>
    /// statements within any case body can branch directly to the end of the switch.
    /// </para>
    /// <para>
    /// The switch expression is evaluated once, boxed, and stored in a temporary local. A label
    /// is pre-allocated for each case, and a separate <c>defaultLabel</c> is defined if a
    /// <c>default</c> branch is present (otherwise it aliases <c>exitLabel</c>).
    /// </para>
    /// <para>
    /// The comparison pass loads the stored switch value alongside each case expression and calls
    /// <c>Runtime.StrictEquals</c> for PHP <c>===</c> semantics. A successful comparison branches
    /// to that case's label; if no case matches, control falls through to <c>defaultLabel</c> or
    /// <c>exitLabel</c>.
    /// </para>
    /// <para>
    /// Case bodies are emitted sequentially, each terminated by an unconditional branch to
    /// <c>exitLabel</c> to prevent fall-through. The <c>default</c> body, if present, is emitted
    /// last and similarly terminated. Finally, <c>exitLabel</c> is marked and popped from
    /// <c>_breakLabels</c>.
    /// </para>
    /// </remarks>
    public void VisitSwitchNode(SwitchNode node, in ReadOnlySpan<char> source)
    {
        var exitLabel = DefineLabel();

        _breakLabels.Push(exitLabel);

        LocalBuilder? switchValue = null;
        if (node.Expression != null)
        {
            node.Expression.Accept(this, source);
            EmitBoxing(node.Expression);
            switchValue = DeclareLocal(typeof(object));
            Emit(OpCodes.Stloc, switchValue);
        }

        var caseLabels = new Label[node.Cases.Count];
        for (int i = 0; i < node.Cases.Count; i++)
            caseLabels[i] = DefineLabel();

        var defaultLabel = node.Default != null ? DefineLabel() : exitLabel;

        // Compare each case value
        for (int i = 0; i < node.Cases.Count; i++)
        {
            var caseNode = node.Cases[i];
            if (caseNode.Expression != null && switchValue != null)
            {
                Emit(OpCodes.Ldloc, switchValue);
                caseNode.Expression.Accept(this, source);
                EmitBoxingIfLiteral(caseNode.Expression);
                var strictEquals = typeof(PHPIL.Engine.Runtime.Runtime).GetMethod("StrictEquals", BindingFlags.Public | BindingFlags.Static);
                Emit(OpCodes.Call, strictEquals!);
                Emit(OpCodes.Brtrue, caseLabels[i]);
            }
        }

        // Branch to default or exit
        if (node.Default != null)
            Emit(OpCodes.Br, defaultLabel);
        else
            Emit(OpCodes.Br, exitLabel);

        // Emit case bodies
        for (int i = 0; i < node.Cases.Count; i++)
        {
            MarkLabel(caseLabels[i]);
            if (node.Cases[i].Body != null)
                node.Cases[i].Body.Accept(this, source);
            Emit(OpCodes.Br, exitLabel); // Prevent fall-through
        }

        // Emit default body
        if (node.Default != null)
        {
            MarkLabel(defaultLabel);
            node.Default.Accept(this, source);
            Emit(OpCodes.Br, exitLabel);
        }

        MarkLabel(exitLabel);

        _breakLabels.Pop();
    }

    /// <summary>
    /// Emits IL for a single <c>case</c> clause body within a <c>switch</c> statement.
    /// </summary>
    /// <param name="node">The <see cref="CaseNode"/> representing the case clause.</param>
    /// <param name="source">The original source text, passed through to the body visitor.</param>
    /// <remarks>
    /// This visitor only emits the case body. Case label definition, expression comparison,
    /// and fall-through prevention are all handled by the enclosing <see cref="VisitSwitchNode"/>.
    /// </remarks>
    public void VisitCaseNode(CaseNode node, in ReadOnlySpan<char> source)
    {
        if (node.Body != null)
            node.Body.Accept(this, source);
    }
}