using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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

    public void VisitCaseNode(CaseNode node, in ReadOnlySpan<char> source)
    {
        if (node.Body != null)
            node.Body.Accept(this, source);
    }
}
