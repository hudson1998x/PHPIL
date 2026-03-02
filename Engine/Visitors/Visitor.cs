using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

/// <summary>
/// A composite visitor that broadcasts each <see cref="SyntaxNode"/> to an
/// ordered collection of inner <see cref="IVisitor"/> implementations.
///
/// <para>
/// This is an application of the <em>Composite</em> pattern: from the outside,
/// a <see cref="Visitor"/> looks identical to any other <see cref="IVisitor"/>,
/// but internally it fans each visit out to multiple concrete visitors — a type
/// checker, a code generator, a pretty-printer, etc. — without the caller needing
/// to know how many visitors are active or iterate over them manually.
/// </para>
///
/// <para>
/// Visitors are applied in the order they are passed to the constructor. This
/// ordering is significant if any visitor produces side effects that a later one
/// depends on (e.g. a symbol-table builder that must run before a type checker).
/// </para>
/// </summary>
public class Visitor : IVisitor
{
    private readonly IVisitor[] _visitors;

    /// <summary>
    /// Initialises the composite with the set of visitors to broadcast to.
    /// </summary>
    /// <param name="visitors">
    /// One or more <see cref="IVisitor"/> implementations, applied in order.
    /// </param>
    public Visitor(params IVisitor[] visitors)
    {
        _visitors = visitors;
    }

    /// <summary>
    /// Dispatches <paramref name="node"/> to every inner visitor via
    /// <see cref="SyntaxNode.Accept"/>. Using <c>Accept</c> rather than calling
    /// <c>visitor.Visit(node, span)</c> directly is what enables double dispatch —
    /// <c>Accept</c> is overridden on each concrete node type to call the most
    /// specific <c>Visit</c> overload available on the visitor, so a
    /// <c>BinaryOpNode</c> routes to <c>Visit(BinaryOpNode, ...)</c> rather than
    /// the base <c>Visit(SyntaxNode, ...)</c> fallback.
    /// </summary>
    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        foreach (var visitor in _visitors)
            node.Accept(visitor, in span);
    }
}