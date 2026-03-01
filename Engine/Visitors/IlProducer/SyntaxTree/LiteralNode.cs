using System;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

public partial class LiteralNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        if (visitor is not IlProducer ilProducer) return;

        var il = ilProducer.GetILGenerator();
        var value = Token.TextValue(source);

        switch (Token.Kind)
        {
            case TokenKind.IntLiteral:
                il.Emit(OpCodes.Ldc_I4, int.Parse(value));   // push int
                il.Emit(OpCodes.Box, typeof(int));           // box to object
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(object) })!); // wrap in PhpValue
                break;

            case TokenKind.FloatLiteral:
                il.Emit(OpCodes.Ldc_R8, double.Parse(value)); // push double
                il.Emit(OpCodes.Box, typeof(double));         // box to object
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(object) })!); // wrap
                break;

            case TokenKind.StringLiteral:
                var str = value.Length >= 2 ? value[1..^1] : value;
                str = str
                    .Replace("\\n", "\n")
                    .Replace("\\t", "\t")
                    .Replace("\\r", "\r")
                    .Replace("\\v", "\v")
                    .Replace("\\e", "\x1B")
                    .Replace("\\f", "\f")
                    .Replace("\\\\", "\\")
                    .Replace("\\$", "$")
                    .Replace("\\\"", "\"")
                    .Replace("\\'", "'");
                il.Emit(OpCodes.Ldstr, str);
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(object) })!);
                break;

            case TokenKind.TrueLiteral:
                il.Emit(OpCodes.Ldc_I4_1);     // push 1
                il.Emit(OpCodes.Box, typeof(bool)); // box to object
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(object) })!);
                break;

            case TokenKind.FalseLiteral:
                il.Emit(OpCodes.Ldc_I4_0);     // push 0
                il.Emit(OpCodes.Box, typeof(bool)); // box to object
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(object) })!);
                break;

            case TokenKind.NullLiteral:
                il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
                break;
        }

        ilProducer.LastEmittedType = typeof(PhpValue);
    }
}