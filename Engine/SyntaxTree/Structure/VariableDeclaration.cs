using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableDeclaration : ExpressionNode
{
    public Token VariableName;
    
    public ExpressionNode? VariableValue { get; set; }
}