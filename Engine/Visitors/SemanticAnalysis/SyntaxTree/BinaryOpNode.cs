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
        if (left == AnalysedType.Mixed || right == AnalysedType.Mixed)
            return AnalysedType.Mixed;

        if (left == AnalysedType.Float || right == AnalysedType.Float)
            return AnalysedType.Float;

        if (left == AnalysedType.Int && right == AnalysedType.Int)
            return AnalysedType.Int;
        
        throw new Exception("Unable to convert " + left + " to " + right);
    }
}