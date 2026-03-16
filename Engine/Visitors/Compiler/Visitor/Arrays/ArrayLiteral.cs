using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL to construct a PHP array literal as a <c>Dictionary&lt;object, object&gt;</c>.
    /// </summary>
    /// <param name="node">The <see cref="ArrayLiteralNode"/> representing the array literal expression.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    /// <remarks>
    /// <para>
    /// A fresh <c>Dictionary&lt;object, object&gt;</c> is allocated and stored in a temporary local.
    /// Each item in <paramref name="node"/> is then processed in order:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b>Spread items</b> (<c>...$arr</c>) — the spread expression is evaluated, cast to
    ///       <c>Dictionary&lt;object, object&gt;</c>, and merged into the accumulator via
    ///       <c>ArrayHelpers.Merge</c>. The result replaces the accumulator.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Keyed items</b> (<c>key =&gt; value</c>) — the key and value are each evaluated and
    ///       boxed if they are <c>int</c> or <c>double</c> literals, then inserted via
    ///       <c>Dictionary.set_Item</c>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Unkeyed items</b> — the value is evaluated and boxed if necessary, then appended
    ///       via <c>ArrayHelpers.Append</c> for PHP-style auto-indexing.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The completed dictionary is left on the stack when the method returns.
    /// </para>
    /// </remarks>
    public void VisitArrayLiteralNode(ArrayLiteralNode node, in ReadOnlySpan<char> source)
    {
        var dictionaryType = typeof(Dictionary<object, object>);
        Emit(OpCodes.Newobj, dictionaryType.GetConstructor(Type.EmptyTypes)!);

        var tempLocal = DeclareLocal(dictionaryType);
        Emit(OpCodes.Stloc, tempLocal);

        var appendMethod = typeof(Runtime.Sdk.ArrayHelpers).GetMethod("Append", new[] { dictionaryType, typeof(object) })!;
        var mergeMethod = typeof(Runtime.Sdk.ArrayHelpers).GetMethod("Merge", new[] { dictionaryType, dictionaryType })!;

        foreach (var item in node.Items)
        {
            // Check if this is a spread item
            if (item.Value is SpreadNode spreadNode)
            {
                // Push the target dictionary (accumulator)
                Emit(OpCodes.Ldloc, tempLocal);
                
                // Evaluate the spread expression (should be an array)
                spreadNode.Expression.Accept(this, source);
                EmitBoxingIfLiteral(spreadNode.Expression);
                
                // Ensure it's a dictionary
                Emit(OpCodes.Castclass, dictionaryType);
                
                // Call Merge - returns the merged dictionary
                Emit(OpCodes.Call, mergeMethod);
                
                // Store result back to tempLocal
                Emit(OpCodes.Stloc, tempLocal);
            }
            else
            {
                // Regular array item
                Emit(OpCodes.Ldloc, tempLocal);

                if (item.Key != null)
                {
                    // Has explicit key - use the key
                    item.Key.Accept(this, source);
                    if (item.Key is LiteralNode { Token.Kind: TokenKind.IntLiteral })
                    {
                        Emit(OpCodes.Box, typeof(int));
                    }
                    else if (item.Key is LiteralNode { Token.Kind: TokenKind.FloatLiteral })
                    {
                        Emit(OpCodes.Box, typeof(double));
                    }
                    
                    var itemSetter = dictionaryType.GetMethod("set_Item", new[] { typeof(object), typeof(object) })!;
                    item.Value.Accept(this, source);
                    if (item.Value is LiteralNode { Token.Kind: TokenKind.IntLiteral })
                    {
                        Emit(OpCodes.Box, typeof(int));
                    }
                    else if (item.Value is LiteralNode { Token.Kind: TokenKind.FloatLiteral })
                    {
                        Emit(OpCodes.Box, typeof(double));
                    }
                    Emit(OpCodes.Callvirt, itemSetter);
                }
                else
                {
                    // No explicit key - use append for auto-indexing
                    item.Value.Accept(this, source);
                    if (item.Value is LiteralNode { Token.Kind: TokenKind.IntLiteral })
                    {
                        Emit(OpCodes.Box, typeof(int));
                    }
                    else if (item.Value is LiteralNode { Token.Kind: TokenKind.FloatLiteral })
                    {
                        Emit(OpCodes.Box, typeof(double));
                    }
                    
                    Emit(OpCodes.Call, appendMethod);
                }
            }
        }

        Emit(OpCodes.Ldloc, tempLocal);
    }

    /// <summary>
    /// Emits IL to evaluate the inner expression of a spread operator (<c>...</c>).
    /// </summary>
    /// <param name="node">The <see cref="SpreadNode"/> representing the spread expression.</param>
    /// <param name="source">The original source text, passed through to the inner expression visitor.</param>
    /// <remarks>
    /// This visitor only evaluates and boxes the spread expression — it does not perform the
    /// actual spreading. The enclosing context (an array literal or function call) is responsible
    /// for consuming the value and merging or unpacking it as appropriate.
    /// </remarks>
    public void VisitSpreadNode(SpreadNode node, in ReadOnlySpan<char> source)
    {
        // Spread node - this is typically used in function calls or array literals
        // The actual spreading logic is handled by the caller (array literal or function call)
        node.Expression.Accept(this, source);
        EmitBoxingIfLiteral(node.Expression);
    }
}