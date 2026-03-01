using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public class FunctionParameter
{
    public Token TypeHint { get; init; }
    public Token Name     { get; init; }
}

public partial class FunctionNode : SyntaxNode
{
    public Token Name                       { get; init; }
    public List<FunctionParameter> Params   { get; init; } = [];
    public BlockNode? Body { get; init; }
    
    public Token? ReturnType          { get; init; }
}