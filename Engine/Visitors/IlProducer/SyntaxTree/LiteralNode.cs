using System;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Visitor implementation for <see cref="LiteralNode"/> — emits IL that pushes
/// a <see cref="PhpValue"/> onto the evaluation stack for each supported literal
/// kind: integer, float, string, boolean, and null.
///
/// <para>
/// All literals are normalised to <see cref="PhpValue"/> regardless of their
/// underlying CLR type. This keeps the rest of the emitter simple — every
/// expression result on the stack is the same type, so binary operations,
/// assignments, and function calls don't need to branch on what the previous
/// node emitted. <see cref="IlProducer.LastEmittedType"/> is set to
/// <see cref="PhpValue"/> unconditionally at the end to reflect this.
/// </para>
/// </summary>
public partial class LiteralNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        // This node only knows how to emit IL — if the visitor isn't an IlProducer
        // (e.g. a pretty-printer or analyser), silently do nothing. Each node type
        // is responsible for deciding which visitor types it supports.
        if (visitor is not IlProducer ilProducer) return;

        var il    = ilProducer.GetILGenerator();
        var value = Token.TextValue(source); // slice the raw text from the source span

        switch (Token.Kind)
        {
            case TokenKind.IntLiteral:
                // PHP integers are emitted as CLR ints wrapped in a PhpValue.
                // We use the specific int constructor directly to avoid unnecessary
                // boxing to object.
                il.Emit(OpCodes.Ldc_I4, int.Parse(value));
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(int) })!);
                break;

            case TokenKind.FloatLiteral:
                // Same pattern as integers, using Ldc_R8 for a 64-bit double 
                // to match PHP's float precision and wrapping via the double constructor.
                il.Emit(OpCodes.Ldc_R8, double.Parse(value));
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(double) })!);
                break;

            case TokenKind.StringLiteral:
                // Strip the surrounding quote characters that the lexer left in
                // the token text, then unescape PHP escape sequences into their
                // real character equivalents.
                var str = value.Length >= 2 ? value[1..^1] : value;
                str = str
                    .Replace("\\n",  "\n")
                    .Replace("\\t",  "\t")
                    .Replace("\\r",  "\r")
                    .Replace("\\v",  "\v")
                    .Replace("\\e",  "\x1B") // ESC character (PHP 5.4+)
                    .Replace("\\f",  "\f")
                    .Replace("\\\\", "\\")   // literal backslash — must come after other \X replacements
                    .Replace("\\$",  "$")    // escaped dollar sign (not a variable)
                    .Replace("\\\"", "\"")
                    .Replace("\\'",  "'");
                il.Emit(OpCodes.Ldstr, str);
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(string) })!);
                break;

            case TokenKind.TrueLiteral:
                // `true` is represented as a PhpValue containing a boolean.
                // Ldc_I4_1 is a single-byte shorthand opcode for pushing the
                // integer 1 (representing true).
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(bool) })!);
                break;

            case TokenKind.FalseLiteral:
                // Mirror of TrueLiteral using Ldc_I4_0 (push 0) for false.
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Newobj, typeof(PhpValue)
                    .GetConstructor(new[] { typeof(bool) })!);
                break;

            case TokenKind.NullLiteral:
                // null is a singleton — loaded directly from a static field on
                // PhpValue rather than constructed. This avoids allocating a new
                // PhpValue instance for every null literal and keeps null identity
                // consistent across the runtime.
                il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
                break;
        }

        // Signal to the parent node that a PhpValue is now on top of the stack.
        // The parent (e.g. BinaryOpNode, VariableAssignment) uses this to decide
        // what operations are valid without needing to re-inspect the token kind.
        ilProducer.LastEmittedType = typeof(PhpValue);
    }
}