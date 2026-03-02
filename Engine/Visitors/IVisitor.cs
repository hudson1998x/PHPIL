using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

/// <summary>
/// Defines the contract for AST visitors — objects that traverse the syntax tree
/// produced by the parser and act on each node.
///
/// <para>
/// Declared as <c>partial</c> so node-specific overloads (e.g.
/// <c>Visit(IfNode node, ...)</c>, <c>Visit(BinaryOpNode node, ...)</c>) can be
/// defined in separate files, one per node type, without this file growing
/// unboundedly as new node types are added. The base overload here accepts any
/// <see cref="SyntaxNode"/> and acts as the fallback for node types that a
/// concrete visitor doesn't specifically handle.
/// </para>
///
/// <para>
/// The <paramref name="span"/> parameter carries the original source text through
/// the traversal so visitors can extract raw string values (identifiers, literals,
/// operator symbols) from token ranges without needing a separate lookup — the
/// token's <c>Start</c> and <c>Length</c> fields index directly into it.
/// </para>
/// </summary>
public partial interface IVisitor
{
    /// <summary>
    /// Visit a syntax tree node.
    /// </summary>
    /// <param name="node">The node being visited.</param>
    /// <param name="span">The original source text. Passed as <c>in</c> to avoid
    /// copying the span at every level of the traversal.</param>
    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span);
}