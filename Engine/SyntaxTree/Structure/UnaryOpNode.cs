using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class UnaryOpNode : ExpressionNode
{
    public TokenKind Operator;
    
    public SyntaxNode? Operand;

    public bool Prefix = false;
}