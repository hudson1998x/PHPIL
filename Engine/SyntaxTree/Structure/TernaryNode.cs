namespace PHPIL.Engine.SyntaxTree;

public partial class TernaryNode : ExpressionNode
{
    public ExpressionNode? Condition { get; init; }
    public ExpressionNode? Then      { get; init; }
    public ExpressionNode? Else      { get; init; }
}