using PHPIL.Engine.CodeLexer;

namespace PHPIL.Engine.SyntaxTree;

public partial class BreakNode : ExpressionNode
{
    /// <summary>
    /// The optional numeric token indicating the nesting level to break out of.
    /// If <c>null</c>, the statement defaults to breaking the innermost loop (level 1).
    /// </summary>
    public Token? Label { get; set; }
}