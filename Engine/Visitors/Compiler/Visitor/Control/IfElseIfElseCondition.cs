using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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