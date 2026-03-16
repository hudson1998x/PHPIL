using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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