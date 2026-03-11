using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source)
    {
        var elseLabel = DefineLabel();
        var endLabel = DefineLabel();

        node.Condition?.Accept(this, source);
        EmitCoerceToBool();
        Emit(OpCodes.Brfalse, elseLabel);

        node.Then?.Accept(this, source);
        EmitBoxingIfLiteral(node.Then);
        Emit(OpCodes.Br, endLabel);

        MarkLabel(elseLabel);
        node.Else?.Accept(this, source);
        EmitBoxingIfLiteral(node.Else);

        MarkLabel(endLabel);
    }
}
