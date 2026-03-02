using System.Reflection.Emit;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;
using PHPIL.Engine.Runtime.Types;

namespace PHPIL.Engine.SyntaxTree
{
    /// <summary>
    /// Visitor implementation for <see cref="IfNode"/> — emits IL for a complete
    /// if/elseif/else chain using forward jump labels to route execution to the
    /// correct branch at runtime.
    ///
    /// <para>
    /// The emitted structure follows the standard conditional branch pattern:
    /// evaluate the condition, convert to bool, branch to the else label if false,
    /// emit the if-body, jump unconditionally to the end label, then emit elseif
    /// and else branches each separated by their own labels. This is the same
    /// pattern a C# compiler produces for an if/else chain.
    /// </para>
    /// </summary>
    public partial class IfNode : SyntaxNode
    {
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            if (visitor is IlProducer ilProducer)
            {
                Accept(ilProducer, in source);
                return;
            }

            base.Accept(visitor, in source);
        }

        /// <summary>
        /// Typed IL emission path. Emits the full if/elseif/else branch structure.
        /// </summary>
        private void Accept(IlProducer ilProducer, in ReadOnlySpan<char> source)
        {
            var il = ilProducer.GetILGenerator();

            // Two labels bracket the entire construct:
            // - elseLabel: where execution jumps if the if-condition is false
            // - endLabel:  where all branches converge after executing their body
            var endLabel  = il.DefineLabel();
            var elseLabel = il.DefineLabel();

            if (Expression != null)
            {
                // Emit the condition expression — leaves a value on the stack.
                ilProducer.Visit(Expression, in source);

                // Normalise to PhpValue if the condition emitted something else,
                // boxing value types first since PhpValue's constructor takes `object`.
                if (ilProducer.LastEmittedType != typeof(PhpValue))
                {
                    if (ilProducer.LastEmittedType != null && ilProducer.LastEmittedType.IsValueType)
                        il.Emit(OpCodes.Box, ilProducer.LastEmittedType);

                    il.Emit(OpCodes.Newobj, typeof(PhpValue).GetConstructor(new[] { typeof(object) })!);
                }

                // Convert to a CLR bool using PHP's truthiness rules (0, "", "0", null,
                // and empty arrays are all falsy) then branch to elseLabel if false.
                il.Emit(OpCodes.Callvirt, typeof(PhpValue).GetMethod("ToBool")!);
                il.Emit(OpCodes.Brfalse, elseLabel);
            }

            // ── If body ───────────────────────────────────────────────────────
            // Only reached when the condition was truthy. After executing, jump
            // unconditionally to endLabel to skip all elseif/else branches.
            Body?.Accept(ilProducer, in source);
            il.Emit(OpCodes.Br, endLabel);

            // ── Elseif / else branches ────────────────────────────────────────
            // elseLabel lands here — the if condition was false. Each ElseIfNode
            // is responsible for emitting its own condition check and body, including
            // its own internal labels, so they chain naturally without extra coordination here.
            il.MarkLabel(elseLabel);
            foreach (var elseif in ElseIfs)
                elseif.Accept(ilProducer, in source);

            // The else block, if present, follows directly after the elseif chain.
            // If no else exists, execution falls through to endLabel with no-op.
            ElseNode?.Accept(ilProducer, in source);

            // ── Convergence point ─────────────────────────────────────────────
            // All branches jump here on completion. Code after the if statement
            // starts from this label regardless of which branch executed.
            il.MarkLabel(endLabel);

            // If statements are not expressions in PHP — they don't leave a value
            // on the stack. Setting LastEmittedType to `object` rather than null
            // signals "something may have been emitted" without asserting a specific
            // type, which is the safest choice given the branches may have left
            // differing types or nothing at all.
            ilProducer.LastEmittedType = typeof(object);
        }
    }
}