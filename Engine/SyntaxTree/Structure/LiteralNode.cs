using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class LiteralNode : ExpressionNode
{
    public Token Token { get; set; }
}