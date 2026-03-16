using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL to evaluate an array access expression and retrieve the value at the specified key.
    /// </summary>
    /// <param name="node">The <see cref="ArrayAccessNode"/> representing the array access expression.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    /// <exception cref="Exception">
    /// Thrown when <paramref name="node"/> has no key, as keyless array access is only valid on the
    /// left-hand side of an assignment (e.g. <c>$arr[] = value</c>).
    /// </exception>
    /// <remarks>
    /// Emission order:
    /// <list type="number">
    ///   <item><description>Evaluate the array expression, leaving a <c>Dictionary&lt;object, object&gt;</c> on the stack.</description></item>
    ///   <item><description>Evaluate the key expression, boxing it if it is a literal value.</description></item>
    ///   <item><description>Call <c>Dictionary&lt;object, object&gt;.get_Item</c> via <see cref="OpCodes.Callvirt"/> to retrieve the value.</description></item>
    /// </list>
    /// </remarks>
    public void VisitArrayAccessNode(ArrayAccessNode node, in ReadOnlySpan<char> source)
    {
        // 1. Evaluate array expression
        node.Array.Accept(this, source);

        // 2. Evaluate key expression (if any - for GET it should have a key)
        if (node.Key == null)
            throw new Exception("Array access without a key is only allowed in assignment.");

        node.Key.Accept(this, source);
        
        // Boxing if necessary (matching ArrayLiteral logic)
        EmitBoxingIfLiteral(node.Key);

        // 3. Call get_Item
        var dictionaryType = typeof(Dictionary<object, object>);
        var getter = dictionaryType.GetMethod("get_Item", new[] { typeof(object) })!;
        Emit(OpCodes.Callvirt, getter);
    }
}