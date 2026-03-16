using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a postfix increment (<c>$x++</c>) or decrement (<c>$x--</c>) expression.
    /// </summary>
    /// <param name="node">The <see cref="PostfixExpressionNode"/> representing the postfix expression.</param>
    /// <param name="source">The original source text, used to resolve the operand variable name.</param>
    /// <exception cref="Exception">
    /// Thrown when the operand is not a <see cref="VariableNode"/>, or when the variable has not
    /// been declared in the current scope.
    /// </exception>
    /// <remarks>
    /// The variable's current (pre-mutation) value is loaded first, leaving the original value on
    /// the stack as the expression result. The value is then unboxed to <see cref="int"/>,
    /// incremented or decremented by one, reboxed, and stored back to the local — preserving
    /// postfix semantics where the caller receives the value before the mutation.
    /// </remarks>
    public void VisitPostfixExpressionNode(PostfixExpressionNode node, in ReadOnlySpan<char> source)
    {
        if (node.Operator.Kind == TokenKind.Increment || node.Operator.Kind == TokenKind.Decrement)
        {
            if (node.Operand is not VariableNode varNode)
                throw new Exception("Increment/Decrement requires a variable.");

            string varName = varNode.Token.TextValue(in source);
            if (!_locals.TryGetValue(varName, out var local))
                throw new Exception($"Undefined variable: {varName}");

            Emit(OpCodes.Ldloc, local);
            Emit(OpCodes.Ldloc, local);
            Emit(OpCodes.Unbox_Any, typeof(int));
            Emit(node.Operator.Kind == TokenKind.Increment ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_M1);
            Emit(OpCodes.Add);
            Emit(OpCodes.Box, typeof(int));
            Emit(OpCodes.Stloc, local);

            // Result on stack: [Old Value]
        }
    }
}