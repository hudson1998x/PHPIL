using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitLiteralNode(LiteralNode node, in ReadOnlySpan<char> source)
    {
        node.AnalysedType = node.Token.Kind switch
        {
            TokenKind.IntLiteral    => AnalysedType.Int,
            TokenKind.FloatLiteral  => AnalysedType.Float,
            TokenKind.StringLiteral => AnalysedType.String,
            TokenKind.TrueLiteral   => AnalysedType.Boolean,
            TokenKind.FalseLiteral  => AnalysedType.Boolean,
            TokenKind.NullLiteral   => AnalysedType.Mixed,
            _                       => AnalysedType.Mixed
        };
    }
}