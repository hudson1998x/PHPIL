namespace PHPIL.Engine.SyntaxTree;

public partial class ElseNode : SyntaxNode
{
    public BlockNode? Body { get; set; }
}