using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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