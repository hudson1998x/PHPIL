using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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