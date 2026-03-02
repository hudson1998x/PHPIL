using System;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Visitor implementation for <see cref="BinaryOpNode"/> — emits IL that evaluates
/// both operands and dispatches to the corresponding <see cref="PhpValue"/> operator
/// method, leaving a <see cref="PhpValue"/> result on the evaluation stack.
///
/// <para>
/// Rather than emitting raw IL arithmetic (e.g. <c>add</c>, <c>mul</c>), all
/// binary operations are delegated to static operator methods on <see cref="PhpValue"/>.
/// This means PHP's dynamic type coercion rules ("1" + 2 = 3, true + true = 2, etc.)
/// are handled entirely within the runtime type system rather than needing to be
/// re-implemented here at the IL level. The emitter stays simple — it just needs to
/// get both operands onto the stack in the right order and call the right method.
/// </para>
///
/// <para>
/// CLR operator overloads (<c>op_Addition</c>, <c>op_Subtraction</c>, etc.) are used
/// where the operator maps cleanly to a CLR convention. Methods with PHP-specific
/// semantics that don't have a direct CLR equivalent (<c>LooseEquals</c>,
/// <c>StrictEquals</c>, <c>Spaceship</c>, etc.) are named methods on
/// <see cref="PhpValue"/> instead.
/// </para>
/// </summary>
public partial class BinaryOpNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        if (visitor is not IlProducer ilProducer) return;

        var il = ilProducer.GetILGenerator();

        // Emit left then right operand — order matters for non-commutative operators
        // (subtraction, division, comparison). Both visit calls leave a PhpValue on
        // the stack, giving the operator method its two arguments in the correct order.
        ilProducer.Visit(Left!,  in source);
        ilProducer.Visit(Right!, in source);
        // Stack: [ PhpValue (left), PhpValue (right) ]

        // Resolve the PhpValue method that implements this operator. Using `Call`
        // rather than `Callvirt` is correct here because all operator methods are
        // static — there's no virtual dispatch involved.
        var method = Operator switch
        {
            // ── Arithmetic ────────────────────────────────────────────────────
            // CLR operator overload naming convention — these map directly to C#
            // operator overloads defined on PhpValue.
            TokenKind.Add      => typeof(PhpValue).GetMethod("op_Addition")!,
            TokenKind.Subtract => typeof(PhpValue).GetMethod("op_Subtraction")!,
            TokenKind.Multiply => typeof(PhpValue).GetMethod("op_Multiply")!,
            TokenKind.DivideBy => typeof(PhpValue).GetMethod("op_Division")!,
            TokenKind.Modulo   => typeof(PhpValue).GetMethod("op_Modulus")!,

            // ── Comparison ────────────────────────────────────────────────────
            TokenKind.LessThan           => typeof(PhpValue).GetMethod("op_LessThan")!,
            TokenKind.GreaterThan        => typeof(PhpValue).GetMethod("op_GreaterThan")!,
            TokenKind.LessThanOrEqual    => typeof(PhpValue).GetMethod("op_LessThanOrEqual")!,
            TokenKind.GreaterThanOrEqual => typeof(PhpValue).GetMethod("op_GreaterThanOrEqual")!,

            // ── Logical ───────────────────────────────────────────────────────
            // Named methods rather than operator overloads — `&&` and `||` have
            // short-circuit semantics in PHP source but here we're post-parse, so
            // both operands are already evaluated. The methods implement the value
            // coercion rules (truthy/falsy) without short-circuiting.
            TokenKind.LogicalAnd => typeof(PhpValue).GetMethod("And")!,
            TokenKind.LogicalOr  => typeof(PhpValue).GetMethod("Or")!,

            // ── Equality ──────────────────────────────────────────────────────
            // PHP distinguishes loose equality (== coerces types: "1" == 1 → true)
            // from strict equality (=== requires same type and value: "1" === 1 → false).
            // These can't be expressed as CLR operator overloads without ambiguity,
            // so they're named methods.
            TokenKind.ShallowEquality   => typeof(PhpValue).GetMethod("LooseEquals")!,
            TokenKind.ShallowInequality => typeof(PhpValue).GetMethod("LooseNotEquals")!,
            TokenKind.DeepEquality      => typeof(PhpValue).GetMethod("StrictEquals")!,
            TokenKind.DeepInequality    => typeof(PhpValue).GetMethod("StrictNotEquals")!,

            // Spaceship operator (<=>): returns -1, 0, or 1. No CLR equivalent.
            TokenKind.Spaceship => typeof(PhpValue).GetMethod("Spaceship")!,

            // ── Bitwise ───────────────────────────────────────────────────────
            TokenKind.BitwiseAnd => typeof(PhpValue).GetMethod("op_BitwiseAnd")!,
            TokenKind.BitwiseOr  => typeof(PhpValue).GetMethod("op_BitwiseOr")!,
            TokenKind.BitwiseXor => typeof(PhpValue).GetMethod("op_ExclusiveOr")!,
            TokenKind.LeftShift  => typeof(PhpValue).GetMethod("ShiftLeft")!,
            TokenKind.RightShift => typeof(PhpValue).GetMethod("ShiftRight")!,

            // Ampersand doubles as both the reference operator and bitwise AND in PHP.
            // In expression context (which is all this node handles) it means bitwise AND.
            TokenKind.Ampersand => typeof(PhpValue).GetMethod("op_BitwiseAnd")!,
            TokenKind.Concat => typeof(PhpValue).GetMethod("Concat"),

            // TokenKind.RightShiftUnsigned => typeof(PhpValue).GetMethod("ShiftRightUnsigned")!,
            // ^ Placeholder for unsigned right shift if PHP ever adds it (it hasn't as of PHP 8.x).

            _ => throw new NotImplementedException($"Operator {Operator} not implemented.")
        };

        // Call the static operator method — pops both operands and pushes the result.
        il.Emit(OpCodes.Call, method);
        // Stack: [ PhpValue (result) ]

        // All operator methods on PhpValue return PhpValue, so the result type is
        // always known without inspecting the operands' types.
        ilProducer.LastEmittedType = typeof(PhpValue);
    }
}