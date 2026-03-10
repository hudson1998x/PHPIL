using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.SyntaxTree;

public partial class LiteralNode
{
    public new bool HasTypeEmission => true;

    public override AnalysedType AnalysedType
    {
        get
        {
            switch (Token.Kind)
            {
                case TokenKind.IntLiteral:
                    return AnalysedType.Int;
                
                case TokenKind.FloatLiteral:
                    return AnalysedType.Float;
                
                case TokenKind.StringLiteral:
                    return AnalysedType.String;
                
                case TokenKind.TrueLiteral:
                case TokenKind.FalseLiteral:
                    return AnalysedType.Boolean;
                
                default:
                    return base.AnalysedType;
            }
        }
    }
}