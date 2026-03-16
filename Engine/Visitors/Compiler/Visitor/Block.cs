using System.Reflection.Emit;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a block of statements, managing stack hygiene between each statement.
    /// </summary>
    /// <param name="node">The <see cref="BlockNode"/> containing the ordered list of statements.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    /// <remarks>
    /// <para>
    /// Statements are emitted in order. If a <see cref="BreakNode"/> or <see cref="ContinueNode"/>
    /// is encountered, emission stops immediately — subsequent statements in the block are
    /// unreachable.
    /// </para>
    /// <para>
    /// For <see cref="ExpressionNode"/> statements, a <see cref="OpCodes.Pop"/> is emitted after
    /// the expression unless the value is known to be absent, specifically:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Control flow expressions (<see cref="For"/>, <see cref="WhileNode"/>,
    ///       <see cref="SwitchNode"/>, <see cref="IfNode"/>) do not leave a value on the stack.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Assignment expressions (<see cref="BinaryOpNode"/> with an assignment operator)
    ///       where <c>NeedsValue</c> is <see langword="false"/> consume their own result.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Function calls whose resolved method has a <see langword="void"/> return type
    ///       leave nothing on the stack.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public void VisitBlockNode(BlockNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this, in source);

            if (stmt is BreakNode || stmt is ContinueNode)
            {
                return;
            }

            if (stmt is ExpressionNode expr)
            {
                bool shouldPop = true;
                
                // Control flow statements don't have return values
                if (expr is For || expr is WhileNode || expr is SwitchNode || expr is IfNode)
                {
                    shouldPop = false;
                }
                else if (expr is BinaryOpNode bin && IsAssignment(bin.Operator) && !bin.NeedsValue)
                {
                    shouldPop = false;
                }
                else if (expr is FunctionCallNode callNode)
                {
                    var func = ResolveFunction(callNode.Callee, in source);
                    if (func != null)
                    {
                        var method = func.MethodInfo ?? func.Method?.Method;
                        if ((method != null && method.ReturnType == typeof(void)) || func.ReturnType == typeof(void))
                        {
                            shouldPop = false;
                        }
                    }
                }
                
                if (shouldPop)
                    Emit(OpCodes.Pop);
            }
        }
    }
}