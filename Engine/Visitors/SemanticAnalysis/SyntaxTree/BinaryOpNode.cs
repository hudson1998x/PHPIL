using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.SyntaxTree;

public partial class BinaryOpNode
{
    public new bool HasTypeEmission => true;

    public override AnalysedType AnalysedType
    {
        get
        {
            if (Left is { } leftNode && Right is { } rightNode)
            {
                return InferBinaryOpType(Operator,  leftNode.AnalysedType, rightNode.AnalysedType);
            }

            if (Left is { } && Left.HasTypeEmission)
            {
                return Left.AnalysedType;
            }

            if (Right is { } && Right.HasTypeEmission)
            {
                return Right.AnalysedType;
            }
            return AnalysedType.Mixed;
        }
    }


    private static AnalysedType InferBinaryOpType(TokenKind op, AnalysedType left, AnalysedType right)
    {
        if (op == TokenKind.AssignEquals) return right;
        if (op == TokenKind.Concat) return AnalysedType.String;
        if (op == TokenKind.NullCoalesce) return AnalysedType.Mixed;
        
        if (op is TokenKind.LessThan or TokenKind.GreaterThan or TokenKind.ShallowEquality)
            return AnalysedType.Boolean;

        if (left == AnalysedType.Float || right == AnalysedType.Float)
            return AnalysedType.Float;

        return AnalysedType.Int;
    }
}