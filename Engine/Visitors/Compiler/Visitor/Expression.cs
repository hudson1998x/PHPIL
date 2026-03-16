using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for an expression node by visiting each of its child statements in order.
    /// </summary>
    /// <param name="node">The <see cref="ExpressionNode"/> containing the child statements.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    public void VisitExpressionNode(ExpressionNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this, in source);
        }
    }
}