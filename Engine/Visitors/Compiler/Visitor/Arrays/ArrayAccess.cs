using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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
