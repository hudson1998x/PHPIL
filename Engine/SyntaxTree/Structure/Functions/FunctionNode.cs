using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public class FunctionParameter
    {
        public Token? TypeHint { get; init; }
        public Token Name     { get; init; }
    }

    public partial class FunctionNode : SyntaxNode
    {
        public Token Name                       { get; init; }
        public List<FunctionParameter> Params   { get; init; } = [];
        public BlockNode? Body { get; init; }
    
        public Token? ReturnType          { get; init; }
    }
}
namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitFunctionParameter(FunctionParameter node, in ReadOnlySpan<char> source);
    }
}