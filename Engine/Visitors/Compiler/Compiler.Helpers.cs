using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    private void EmitBoxingIfLiteral(SyntaxNode? node)
    {
        if (node is LiteralNode literal)
        {
            if (literal.Token.Kind == TokenKind.IntLiteral)
            {
                Emit(OpCodes.Box, typeof(int));
            }
            else if (literal.Token.Kind == TokenKind.FloatLiteral)
            {
                Emit(OpCodes.Box, typeof(double));
            }
            else if (literal.Token.Kind == TokenKind.TrueLiteral || literal.Token.Kind == TokenKind.FalseLiteral)
            {
                Emit(OpCodes.Box, typeof(bool));
            }
        }
    }
}
