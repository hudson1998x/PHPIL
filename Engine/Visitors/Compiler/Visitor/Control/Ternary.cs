using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a ternary expression (<c>condition ? then : else</c>).
    /// </summary>
    /// <param name="node">The <see cref="TernaryNode"/> representing the ternary expression.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    /// <remarks>
    /// The condition is evaluated and coerced to <see cref="bool"/> via <c>EmitCoerceToBool</c>.
    /// If <see langword="false"/>, control branches to <c>elseLabel</c> where the else expression
    /// is evaluated and boxed if necessary. Otherwise the then expression is evaluated, boxed if
    /// necessary, and an unconditional branch to <c>endLabel</c> skips the else branch. In both
    /// cases the resulting value is left on the stack.
    /// </remarks>
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