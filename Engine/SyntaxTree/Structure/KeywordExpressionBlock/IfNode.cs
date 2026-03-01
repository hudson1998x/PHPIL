namespace PHPIL.Engine.SyntaxTree;

public partial class IfNode : SyntaxNode
{
    public ExpressionNode? Expression;
    
    public BlockNode? Body;
    
    public List<ElseIfNode> ElseIfs = [];
    
    public SyntaxNode? ElseNode;
}