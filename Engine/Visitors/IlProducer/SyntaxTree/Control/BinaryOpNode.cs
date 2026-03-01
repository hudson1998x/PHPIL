using System;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

public partial class BinaryOpNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        if (visitor is not IlProducer ilProducer) return;

        var il = ilProducer.GetILGenerator();

        // Emit left operand
        ilProducer.Visit(Left!, in source);
        // Emit right operand
        ilProducer.Visit(Right!, in source);

        // Call PhpValue operator
        var method = Operator switch
        {
            TokenKind.Add => typeof(PhpValue).GetMethod("op_Addition")!,
            TokenKind.Subtract => typeof(PhpValue).GetMethod("op_Subtraction")!,
            TokenKind.Multiply => typeof(PhpValue).GetMethod("op_Multiply")!,
            TokenKind.DivideBy => typeof(PhpValue).GetMethod("op_Division")!,
            TokenKind.Modulo => typeof(PhpValue).GetMethod("op_Modulus")!,
            TokenKind.LessThan => typeof(PhpValue).GetMethod("op_LessThan")!,
            TokenKind.GreaterThan => typeof(PhpValue).GetMethod("op_GreaterThan")!,
            TokenKind.LessThanOrEqual => typeof(PhpValue).GetMethod("op_LessThanOrEqual")!,
            TokenKind.GreaterThanOrEqual => typeof(PhpValue).GetMethod("op_GreaterThanOrEqual")!,
            TokenKind.LogicalAnd => typeof(PhpValue).GetMethod("And")!,
            TokenKind.LogicalOr => typeof(PhpValue).GetMethod("Or")!,
            TokenKind.ShallowEquality => typeof(PhpValue).GetMethod("LooseEquals")!,
            TokenKind.ShallowInequality => typeof(PhpValue).GetMethod("LooseNotEquals")!,
            TokenKind.DeepEquality => typeof(PhpValue).GetMethod("StrictEquals")!,
            TokenKind.DeepInequality => typeof(PhpValue).GetMethod("StrictNotEquals")!,
            TokenKind.Spaceship => typeof(PhpValue).GetMethod("Spaceship")!,
            TokenKind.BitwiseAnd => typeof(PhpValue).GetMethod("op_BitwiseAnd")!,
            TokenKind.BitwiseOr => typeof(PhpValue).GetMethod("op_BitwiseOr")!,
            TokenKind.BitwiseXor => typeof(PhpValue).GetMethod("op_ExclusiveOr")!,
            TokenKind.LeftShift => typeof(PhpValue).GetMethod("ShiftLeft")!,
            TokenKind.RightShift => typeof(PhpValue).GetMethod("ShiftRight")!,
            TokenKind.Ampersand => typeof(PhpValue).GetMethod("op_BitwiseAnd")!,
            // TokenKind.RightSh => typeof(PhpValue).GetMethod("ShiftRightUnsigned")!,
            _ => throw new NotImplementedException($"Operator {Operator} not implemented.")
        };

        il.Emit(OpCodes.Call, method);

        ilProducer.LastEmittedType = typeof(PhpValue);
    }
}