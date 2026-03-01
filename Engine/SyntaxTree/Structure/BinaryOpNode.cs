using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class BinaryOpNode : ExpressionNode
{
    public SyntaxNode? Left;
    public SyntaxNode? Right;

    public TokenKind Operator;
}