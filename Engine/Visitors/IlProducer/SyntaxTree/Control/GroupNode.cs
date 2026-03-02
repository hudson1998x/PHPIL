using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Visitor implementation for <see cref="GroupNode"/> — transparently delegates
/// to the inner expression, preserving the parenthesised grouping in the AST
/// without emitting any IL of its own.
///
/// <para>
/// Parentheses in PHP (and most languages) affect parse-time precedence but have
/// no runtime meaning — <c>(a + b) * c</c> and <c>a + b * c</c> produce different
/// trees but the grouping node itself contributes nothing to execution. The node
/// exists in the AST to faithfully represent the source structure for tools that
/// care (pretty-printers, source mappers), while this Accept simply unwraps it so
/// the IL emitter never sees it.
/// </para>
/// </summary>
public partial class GroupNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        // Null guard for robustness — a GroupNode with no inner expression shouldn't
        // crash the traversal, though the parser should never produce one.
        if (Inner is not null)
        {
            visitor.Visit(Inner, in source);
        }
    }
}