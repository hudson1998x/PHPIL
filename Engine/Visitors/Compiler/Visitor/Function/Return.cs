using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a <c>return</c> statement.
    /// </summary>
    /// <param name="node">The <see cref="ReturnNode"/> representing the return statement.</param>
    /// <param name="source">The original source text, passed through to the expression visitor.</param>
    /// <remarks>
    /// If a return expression is present, it is evaluated and coerced to the enclosing function's
    /// <c>ReturnType</c> via <c>EmitCoercion</c> before <see cref="OpCodes.Ret"/> is emitted.
    /// A bare <c>return</c> with no expression emits only <see cref="OpCodes.Ret"/>.
    /// </remarks>
    public void VisitReturnNode(ReturnNode node, in ReadOnlySpan<char> source)
    {
        if (node.Expression != null)
        {
            node.Expression.Accept(this, source);
            EmitCoercion(node.Expression.AnalysedType, ReturnType);
        }
        
        Emit(OpCodes.Ret);
    }
}