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

        int nextAutoIndex = 0;
        var itemSetter = dictionaryType.GetMethod("set_Item", new[] { typeof(object), typeof(object) })!;

        foreach (var item in node.Items)
        {
            Emit(OpCodes.Ldloc, tempLocal);

            if (item.Key != null)
            {
                item.Key.Accept(this, source);
                if (item.Key is LiteralNode { Token.Kind: TokenKind.IntLiteral })
                {
                    Emit(OpCodes.Box, typeof(int));
                }
                else if (item.Key is LiteralNode { Token.Kind: TokenKind.FloatLiteral })
                {
                    Emit(OpCodes.Box, typeof(double));
                }
            }
            else
            {
                Emit(OpCodes.Ldc_I4, nextAutoIndex);
                Emit(OpCodes.Box, typeof(int));
                nextAutoIndex++;
            }

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

        Emit(OpCodes.Ldloc, tempLocal);
    }
}