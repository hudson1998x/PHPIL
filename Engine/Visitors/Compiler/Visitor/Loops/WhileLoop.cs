using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitWhileNode(WhileNode node, in ReadOnlySpan<char> source)
    {
        var conditionLabel = DefineLabel();
        var exitLabel = DefineLabel();

        // 1. Start Loop
        MarkLabel(conditionLabel);

        // 2. Condition
        if (node.Expression != null)
        {
            node.Expression.Accept(this, source);
            if (node.Expression is VariableNode)
                Emit(OpCodes.Unbox_Any, typeof(int));
            Emit(OpCodes.Brfalse, exitLabel);
        }

        // 3. Body
        if (node.Body != null)
            node.Body.Accept(this, source);

        // 4. Loop
        Emit(OpCodes.Br, conditionLabel);

        // 5. Exit
        MarkLabel(exitLabel);
    }
}