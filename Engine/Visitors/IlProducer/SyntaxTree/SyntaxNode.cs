using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// The root of the AST node hierarchy. All concrete node types inherit from this class.
///
/// <para>
/// Declared <c>partial</c> so that node-specific properties, child lists, and any
/// additional behaviour can be defined in separate files per node type, keeping the
/// hierarchy organised without one monolithic file.
/// </para>
/// </summary>
public partial class SyntaxNode
{
    /// <summary>
    /// The entry point for the visitor pattern's double dispatch mechanism.
    /// Calls <see cref="IVisitor.Visit"/> with <c>this</c>, which — because
    /// <c>this</c> is the concrete type at the call site — allows the visitor
    /// to resolve to the most specific <c>Visit</c> overload available for that
    /// node type.
    ///
    /// <para>
    /// Each concrete node type overrides this method to pass itself as its own
    /// type rather than as <see cref="SyntaxNode"/>, which is what makes the
    /// double dispatch work. Without the override, every node would call
    /// <c>visitor.Visit(SyntaxNode, ...)</c> and the visitor would always fall
    /// through to the base overload, defeating the purpose of having typed overloads
    /// at all. The base implementation here acts as the fallback for any node type
    /// that doesn't override <c>Accept</c> — useful during development when a new
    /// node type hasn't yet had its visitor wiring added.
    /// </para>
    /// </summary>
    public virtual void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        visitor.Visit(this, in source);
    }
}