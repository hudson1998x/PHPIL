namespace PHPIL.Engine.SyntaxTree;

public partial class GroupNode : ExpressionNode
{
    public ExpressionNode? Inner { get; init; }
}