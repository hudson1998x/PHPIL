using System;
using System.Reflection.Emit;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class WhileNode
    {
        /// <summary>
        /// Accepts a visitor to either traverse the syntax tree or emit Intermediate Language (IL)
        /// for this <c>while</c> loop node.
        /// </summary>
        /// <param name="visitor">
        /// The visitor processing this node. Only <see cref="IlProducer"/> will generate IL;
        /// other visitors will simply traverse child nodes.
        /// </param>
        /// <param name="source">
        /// The source code span corresponding to this node, used for token and variable resolution.
        /// </param>
        /// <remarks>
        /// <para>
        /// For <see cref="IlProducer"/>, this method emits IL for a standard PHP-style <c>while</c> loop:
        /// </para>
        /// <list type="number">
        /// <item>
        /// <description>Defines labels for the start, end (break), and continue points of the loop.</description>
        /// </item>
        /// <item>
        /// <description>Pushes a <see cref="LoopContext"/> onto the producer's control flow stack.</description>
        /// </item>
        /// <item>
        /// <description>Evaluates the loop condition and branches to the end if false.</description>
        /// </item>
        /// <item>
        /// <description>Visits the loop body, allowing nested break/continue statements to resolve their jump targets.</description>
        /// </item>
        /// <item>
        /// <description>Cleans up the control flow stack upon exiting the loop.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
        {
            if (visitor is not IlProducer ilProducer)
            {
                // Non-IL visitors just traverse
                Expression?.Accept(visitor, source);
                Body?.Accept(visitor, source);
                return;
            }

            var il = ilProducer.GetILGenerator();

            // 1. Define labels for the loop structure.
            // In a while loop, the 'continue' target is the same as the 'condition' check.
            Label loopStart = il.DefineLabel();
            Label loopEnd = il.DefineLabel();

            // 2. Register this loop in the producer's loop stack.
            // This allows 'BreakNode' and 'ContinueNode' to find where to jump.
            // For a 'while', continuing means jumping back to the condition check.
            ilProducer.PushLoop(new LoopContext(loopEnd, loopStart));

            // 3. Mark start of loop (Condition Check)
            il.MarkLabel(loopStart);

            // 4. Evaluate condition
            if (Expression != null)
            {
                Expression.Accept(visitor, source);
                // Convert PhpValue to bool for the jump
                il.Emit(OpCodes.Callvirt, typeof(PhpValue).GetMethod("ToBool")!);
                il.Emit(OpCodes.Brfalse, loopEnd);
            }

            // 5. Visit body
            // Any BreakNode or ContinueNode visited inside here will use the labels we just pushed.
            Body?.Accept(visitor, source);

            // 6. Jump back to start to re-evaluate condition
            il.Emit(OpCodes.Br, loopStart);

            // 7. Mark the exit point (Break Target)
            il.MarkLabel(loopEnd);

            // 8. Clean up the loop stack now that we are exiting the loop's scope.
            ilProducer.PopLoop();

            // 9. CRITICAL: A while loop is a statement. It leaves nothing on the stack.
            ilProducer.LastEmittedType = null;
        }
    }
}