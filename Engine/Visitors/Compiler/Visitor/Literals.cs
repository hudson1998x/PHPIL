using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitLiteralNode(LiteralNode node, in ReadOnlySpan<char> source)
    {
        switch (node.Token.Kind)
        {
            case TokenKind.TrueLiteral:   Emit(OpCodes.Ldc_I4_1); break;
            case TokenKind.FalseLiteral:  Emit(OpCodes.Ldc_I4_0); break;
            case TokenKind.StringLiteral: Emit(OpCodes.Ldstr, node.Token.TextValue(in source).Trim(['"', '\''])); break;
            case TokenKind.NullLiteral:   Emit(OpCodes.Ldnull); break;
            case TokenKind.IntLiteral:    Emit(OpCodes.Ldc_I4, Int32.Parse(node.Token.TextValue(in source))); break;
            case TokenKind.FloatLiteral:  Emit(OpCodes.Ldc_R8, double.Parse(node.Token.TextValue(in source))); break;
            default: throw new NotImplementedException();
        }
    }
}