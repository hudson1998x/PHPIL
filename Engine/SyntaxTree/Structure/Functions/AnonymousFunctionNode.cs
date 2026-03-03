// AnonymousFunctionNode.cs

using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class AnonymousFunctionNode : ExpressionNode
    {
        public List<FunctionParameter> Params      { get; init; } = [];
        public List<UseCapture>        UseCaptures { get; init; } = [];
        public Token?                   ReturnType  { get; init; }  // default if absent
        public BlockNode?              Body        { get; init; }
    }

    public class UseCapture
    {
        public Token Name  { get; init; }
        public bool  ByRef { get; init; }
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitAnonymousFunctionNode(AnonymousFunctionNode node, in ReadOnlySpan<char> source);
    }
}