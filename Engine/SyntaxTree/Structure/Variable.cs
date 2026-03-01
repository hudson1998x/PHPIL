using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableNode : ExpressionNode
{
    public Token Token { get; set; }
}