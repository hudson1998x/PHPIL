using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitForNode(For node, in ReadOnlySpan<char> source)
    {
        var conditionLabel = DefineLabel();
        var exitLabel = DefineLabel();

        if (node.Init != null)
            node.Init.Accept(this, source);

        MarkLabel(conditionLabel);

        if (node.Condition != null)
        {
            node.Condition.Accept(this, source);
            Emit(OpCodes.Brfalse, exitLabel);
        }

        if (node.Body != null)
            node.Body.Accept(this, source);

        if (node.Increment != null)
        {
            node.Increment.Accept(this, source);
            Emit(OpCodes.Pop);
        }

        Emit(OpCodes.Br, conditionLabel);

        MarkLabel(exitLabel);
    }
}