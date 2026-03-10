using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitWhileNode(WhileNode node, in ReadOnlySpan<char> source)
    {
        var conditionLabel = DefineLabel();
        var exitLabel = DefineLabel();

        _breakLabels.Push(exitLabel);
        _continueLabels.Push(conditionLabel);

        MarkLabel(conditionLabel);

        if (node.Expression != null)
        {
            node.Expression.Accept(this, source);
            if (node.Expression is VariableNode)
                Emit(OpCodes.Unbox_Any, typeof(int));
            Emit(OpCodes.Brfalse, exitLabel);
        }

        if (node.Body != null)
            node.Body.Accept(this, source);

        Emit(OpCodes.Br, conditionLabel);

        MarkLabel(exitLabel);

        _breakLabels.Pop();
        _continueLabels.Pop();
    }
}