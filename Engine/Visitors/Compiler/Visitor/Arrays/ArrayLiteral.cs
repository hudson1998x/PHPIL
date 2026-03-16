using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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

    public void VisitSpreadNode(SpreadNode node, in ReadOnlySpan<char> source)
    {
        // Spread node - this is typically used in function calls or array literals
        // The actual spreading logic is handled by the caller (array literal or function call)
        node.Expression.Accept(this, source);
        EmitBoxingIfLiteral(node.Expression);
    }
}