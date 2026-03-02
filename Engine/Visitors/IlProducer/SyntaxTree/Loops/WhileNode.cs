using System;
using System.Reflection.Emit;
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
        /// <description>Defines labels for the start and end of the loop.</description>
        /// </item>
        /// <item>
        /// <description>Marks the loop start label.</description>
        /// </item>
        /// <item>
        /// <description>Evaluates the loop condition, leaving a <see cref="PHPIL.Engine.Runtime.Types.PhpValue"/> on the stack.</description>
        /// </item>
        /// <item>
        /// <description>Converts the <see cref="PhpValue"/> to a <c>bool</c> and branches to the end if false.</description>
        /// </item>
        /// <item>
        /// <description>Visits the loop body, emitting IL for each statement.</description>
        /// </item>
        /// <item>
        /// <description>Branches back to the loop start to continue iteration.</description>
        /// </item>
        /// <item>
        /// <description>Marks the loop end label. Ensures that the loop leaves no value on the evaluation stack, since <c>while</c> is a statement.</description>
        /// </item>
        /// </list>
        /// <para>
        /// If the visitor is not an <see cref="IlProducer"/>, the method recursively traverses
        /// <see cref="Expression"/> and <see cref="Body"/> without emitting IL.
        /// </para>
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

            // 1. Define labels
            Label loopStart = il.DefineLabel();
            Label loopEnd = il.DefineLabel();

            // 2. Mark start of loop
            il.MarkLabel(loopStart);

            // 3. Evaluate condition
            Expression?.Accept(visitor, source); 
            // Stack: [PhpValue]

            // Convert PhpValue to bool for the jump
            il.Emit(OpCodes.Callvirt, typeof(PHPIL.Engine.Runtime.Types.PhpValue).GetMethod("ToBool")!);
            il.Emit(OpCodes.Brfalse, loopEnd);

            // 4. Visit body
            Body?.Accept(visitor, source);

            // 5. Jump back to start
            il.Emit(OpCodes.Br, loopStart);

            // 6. Mark end
            il.MarkLabel(loopEnd);

            // 7. CRITICAL: A while loop is a statement. It leaves nothing on the stack.
            ilProducer.LastEmittedType = null;
        }
    }
}