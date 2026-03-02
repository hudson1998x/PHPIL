using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Visitor implementation for <see cref="ExpressionNode"/> — visits each child
/// statement in order, forwarding to the visitor without any additional logic.
///
/// <para>
/// <see cref="ExpressionNode"/> acts as a transparent container: in contexts where
/// a single expression position in the grammar can hold multiple sequential
/// sub-expressions (e.g. a comma expression), each is visited in source order.
/// The visitor and source span are passed through unchanged — this node adds no
/// scope, no IL instructions, and no type tracking of its own.
/// </para>
/// </summary>
public partial class ExpressionNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        foreach (var syntaxNode in Statements)
        {
            visitor.Visit(syntaxNode, in source);
        }
    }
}