using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Visitor implementation for <see cref="BlockNode"/> — iterates over the
/// block's statement list and visits each one in order, wrapping the traversal
/// in a scope push/pop when emitting IL.
///
/// <para>
/// The scope management is IL-emitter-specific: <see cref="IlProducer.EnterScope"/>
/// and <see cref="ExitScope"/> bracket the statement list so that any locals
/// declared inside the block are registered in their own frame and don't leak into
/// the enclosing scope. Non-IL visitors (analysers, pretty-printers) traverse the
/// statements without any scope overhead since the push/pop is guarded by the
/// <c>is IlProducer</c> check.
/// </para>
///
/// <para>
/// Note that the visitor is checked twice — once before the loop and once after.
/// This is intentional: the two checks use different variable names (<c>ilProducer</c>
/// and <c>scope</c>) to satisfy the compiler's definite assignment rules, since a
/// pattern-matched variable from before a loop isn't in scope after it. A single
/// boolean flag captured before the loop would be a cleaner alternative if the
/// double cast becomes a maintenance concern.
/// </para>
/// </summary>
public partial class BlockNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        // Push a new variable scope frame before visiting any statements.
        // This ensures locals declared inside the block don't outlive it —
        // e.g. a variable declared inside an `if` body shouldn't be resolvable
        // by code after the closing brace.
        if (visitor is IlProducer ilProducer)
        {
            ilProducer.EnterScope();
        }

        // Visit each statement in source order. The visitor handles dispatch —
        // each node's Accept routes to the correct typed overload via double dispatch.
        foreach (var syntaxNode in Statements)
        {
            visitor.Visit(syntaxNode, in source);
        }

        // Pop the scope frame now that all statements have been emitted.
        // Must be paired with every EnterScope — an unbalanced push/pop would
        // corrupt variable resolution for any code that follows this block.
        if (visitor is IlProducer scope)
        {
            scope.ExitScope();
        }
    }
}