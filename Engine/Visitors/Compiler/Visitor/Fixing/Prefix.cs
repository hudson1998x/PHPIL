using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a prefix expression, handling increment/decrement, logical not, and unary plus/minus.
    /// </summary>
    /// <param name="node">The <see cref="PrefixExpressionNode"/> representing the prefix expression.</param>
    /// <param name="source">The original source text, used to resolve variable names.</param>
    /// <remarks>
    /// Operator behaviour:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b><c>++</c> / <c>--</c></b> — the operand must be a <see cref="VariableNode"/>. The
    ///       variable is unboxed, incremented or decremented by one, reboxed, duplicated on the
    ///       stack, and stored back to the local. The updated value is left as the expression
    ///       result, reflecting prefix semantics.  If the variable has not yet been declared it is
    ///       implicitly allocated as <see cref="object"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b><c>!</c></b> — the operand is evaluated, coerced to <see cref="bool"/> via
    ///       <c>EmitCoerceToBool</c>, compared equal to zero to invert it, and reboxed as
    ///       <see cref="bool"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Unary <c>-</c></b> — the operand is evaluated, unboxed to <see cref="int"/>,
    ///       negated via <see cref="OpCodes.Neg"/>, and reboxed.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Unary <c>+</c></b> — the operand is evaluated and left on the stack unchanged.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public void VisitPrefixExpressionNode(PrefixExpressionNode node, in ReadOnlySpan<char> source)
    {
        if (node.Operator.Kind == TokenKind.Increment || node.Operator.Kind == TokenKind.Decrement)
        {
            if (node.Operand is VariableNode varNode)
            {
                var varName = varNode.Token.TextValue(in source);
                if (!_locals.TryGetValue(varName, out var local))
                {
                    local = DeclareLocal(typeof(object));
                    _locals[varName] = local;
                }

                Emit(OpCodes.Ldloc, local);
                Emit(OpCodes.Unbox_Any, typeof(int));
                Emit(node.Operator.Kind == TokenKind.Increment ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_M1);
                Emit(OpCodes.Add);
                Emit(OpCodes.Box, typeof(int));
                Emit(OpCodes.Dup);
                Emit(OpCodes.Stloc, local);
                // Result remains on stack
            }
        }
        else if (node.Operator.Kind == TokenKind.Not)
        {
            node.Operand.Accept(this, source);
            EmitCoerceToBool();
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Ceq);
            Emit(OpCodes.Box, typeof(bool));
        }
        else if (node.Operator.Kind == TokenKind.Subtract)
        {
            node.Operand.Accept(this, source);
            Emit(OpCodes.Unbox_Any, typeof(int));
            Emit(OpCodes.Neg);
            Emit(OpCodes.Box, typeof(int));
        }
        else if (node.Operator.Kind == TokenKind.Add)
        {
            node.Operand.Accept(this, source);
        }
    }

    /// <summary>
    /// Emits IL for a generic unary operator expression, currently handling prefix-position
    /// logical not and unary plus/minus.
    /// </summary>
    /// <param name="node">The <see cref="UnaryOpNode"/> representing the unary expression.</param>
    /// <param name="source">The original source text, passed through to the operand visitor.</param>
    /// <remarks>
    /// Only prefix operators are currently handled. Operator behaviour mirrors
    /// <see cref="VisitPrefixExpressionNode"/>: <c>!</c> coerces and inverts to a boxed
    /// <see cref="bool"/>, unary <c>-</c> unboxes, negates, and reboxes as <see cref="int"/>,
    /// and unary <c>+</c> is a no-op beyond evaluating the operand.
    /// </remarks>
    public void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source)
    {
        if (node.Prefix)
        {
            if (node.Operator == TokenKind.Not)
            {
                node.Operand?.Accept(this, source);
                EmitCoerceToBool();
                Emit(OpCodes.Ldc_I4_0);
                Emit(OpCodes.Ceq);
                Emit(OpCodes.Box, typeof(bool));
            }
            else if (node.Operator == TokenKind.Subtract)
            {
                node.Operand?.Accept(this, source);
                Emit(OpCodes.Unbox_Any, typeof(int));
                Emit(OpCodes.Neg);
                Emit(OpCodes.Box, typeof(int));
            }
            else if (node.Operator == TokenKind.Add)
            {
                node.Operand?.Accept(this, source);
            }
        }
    }
}