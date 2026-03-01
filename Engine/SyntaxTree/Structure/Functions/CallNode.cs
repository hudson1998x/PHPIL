namespace PHPIL.Engine.SyntaxTree;

public partial class FunctionCallNode : ExpressionNode
{
    public ExpressionNode? Callee { get; init; }  // what's being called
    public List<ExpressionNode> Args { get; init; } = [];
}