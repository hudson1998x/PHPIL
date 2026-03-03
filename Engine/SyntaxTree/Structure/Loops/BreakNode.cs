using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.SyntaxTree
{
    public partial class BreakNode : ExpressionNode
    {
        /// <summary>
        /// The optional numeric token indicating the nesting level to break out of.
        /// If <c>null</c>, the statement defaults to breaking the innermost loop (level 1).
        /// </summary>
        public Token? Label { get; set; }
    }
}

namespace PHPIL.Engine.Visitors
{
    public partial interface IVisitor
    {
        void VisitBreakNode(BreakNode node, in ReadOnlySpan<char> source);
    }
}