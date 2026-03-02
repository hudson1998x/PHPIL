using System;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree
{
    /// <summary>
    /// Represents a unary operation node in the PHPIL abstract syntax tree (AST).
    /// This node encapsulates operations that take a single operand, such as increment (++),
    /// decrement (--), unary plus (+), unary minus (-), and logical negation (!).
    /// </summary>
    public partial class UnaryOpNode
    {
        /// <summary>
        /// Accepts a visitor and emits the corresponding Intermediate Language (IL) for this unary operation.
        /// Supports prefix and postfix forms of increment and decrement for variables, as well as
        /// unary plus, unary minus, and logical NOT operations.
        /// </summary>
        /// <param name="visitor">
        /// The visitor instance processing this node. Only <see cref="IlProducer"/> is supported for IL generation.
        /// </param>
        /// <param name="source">
        /// A <see cref="ReadOnlySpan{T}"/> containing the source code characters from which tokens are extracted.
        /// Used for variable name resolution.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// Thrown when increment or decrement is applied to a non-variable operand.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when a variable referenced in an increment/decrement operation cannot be found in the current IL context.
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// Thrown when the unary operator is not supported by this implementation.
        /// </exception>
        /// <remarks>
        /// <para>
        /// <b>Increment/Decrement:</b> Handles both prefix (++$var) and postfix ($var++) forms.
        /// The IL stack is manipulated to preserve the correct value semantics depending on prefix/postfix.
        /// </para>
        /// <para>
        /// <b>Unary Plus/Minus:</b> Calls <see cref="PhpValue.UnaryPlus"/> or <see cref="PhpValue.UnaryMinus"/>
        /// on the operand after evaluating it.
        /// </para>
        /// <para>
        /// <b>Logical NOT:</b> Calls <see cref="PhpValue.Not"/> to negate the operand.
        /// </para>
        /// <para>
        /// The type of the last emitted IL value is stored in <c>ilProducer.LastEmittedType</c> for downstream nodes.
        /// </para>
        /// </remarks>
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            if (visitor is not IlProducer ilProducer)
                return;

            var il = ilProducer.GetILGenerator();

            switch (Operator)
            {
                case TokenKind.Increment:
                case TokenKind.Decrement:
                {
                    if (Operand is not VariableNode variable)
                        throw new NotSupportedException("Only variable increment/decrement is supported.");

                    var varName = variable.Token.TextValue(in source);

                    if (!ilProducer.GetContext().TryGetVariableSlot(varName, out var slot))
                        throw new Exception($"Variable {varName} not found.");

                    var opMethod = Operator == TokenKind.Increment
                        ? typeof(PhpValue).GetMethod("op_Addition", new[] { typeof(PhpValue), typeof(PhpValue) })!
                        : typeof(PhpValue).GetMethod("op_Subtraction", new[] { typeof(PhpValue), typeof(PhpValue) })!;

                    if (Prefix)
                    {
                        // ++$num
                        il.Emit(OpCodes.Ldloc, slot);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Newobj, typeof(PhpValue).GetConstructor(new[] { typeof(int) })!);
                        il.Emit(OpCodes.Call, opMethod);
                        il.Emit(OpCodes.Dup);         // Keep result for expression
                        il.Emit(OpCodes.Stloc, slot); // Store result in variable
                    }
                    else
                    {
                        // $num++
                        il.Emit(OpCodes.Ldloc, slot);   // Load current value (to return as result)
                        il.Emit(OpCodes.Dup);           // Duplicate for increment/decrement
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Newobj, typeof(PhpValue).GetConstructor(new[] { typeof(int) })!);
                        il.Emit(OpCodes.Call, opMethod);
                        il.Emit(OpCodes.Stloc, slot);   // Store updated value
                        // Stack still contains the original value for postfix semantics
                    }

                    ilProducer.LastEmittedType = typeof(PhpValue);
                    break;
                }

                case TokenKind.Add:
                case TokenKind.Subtract:
                {
                    Operand!.Accept(ilProducer, in source);
                    var opMethod = Operator == TokenKind.Add
                        ? typeof(PhpValue).GetMethod("UnaryPlus")!
                        : typeof(PhpValue).GetMethod("UnaryMinus")!;
                    il.Emit(OpCodes.Call, opMethod);
                    ilProducer.LastEmittedType = typeof(PhpValue);
                    break;
                }

                case TokenKind.Not:
                {
                    Operand!.Accept(ilProducer, in source);
                    il.Emit(OpCodes.Call, typeof(PhpValue).GetMethod("Not")!);
                    ilProducer.LastEmittedType = typeof(PhpValue);
                    break;
                }

                default:
                    throw new NotImplementedException($"Unary operator {Operator} not implemented.");
            }
        }
    }
}