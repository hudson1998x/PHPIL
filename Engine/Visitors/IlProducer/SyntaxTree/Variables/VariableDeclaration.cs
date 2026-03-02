using System.Reflection.Emit;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;
using PHPIL.Engine.Runtime;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Visitor implementation for <see cref="VariableDeclaration"/> — emits IL that
/// evaluates the right-hand side expression and stores the result into a local
/// variable slot, declaring the slot if this is the variable's first assignment.
///
/// <para>
/// PHP has no explicit variable declaration syntax — a variable comes into
/// existence the first time it is assigned. This production models that behaviour:
/// the first assignment to a name allocates an IL local and registers it in the
/// current scope frame; subsequent assignments to the same name reuse the existing
/// slot. The result is standard PHP semantics with no special "declare before use"
/// requirement.
/// </para>
/// </summary>
public partial class VariableDeclaration
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        // Delegate to the typed overload if the visitor is an IlProducer.
        // The pattern matches here rather than in the private method so the
        // public Accept signature stays consistent with the rest of the node hierarchy.
        if (visitor is IlProducer ilProducer)
        {
            Accept(ilProducer, source);
        }
    }

    /// <summary>
    /// Typed IL emission path. Split from the public <c>Accept</c> override to
    /// avoid repeated casting and to keep the IL emission logic readable without
    /// the visitor type-check noise at the top.
    /// </summary>
    private void Accept(IlProducer ilProducer, in ReadOnlySpan<char> source)
    {
        var generator = ilProducer.GetILGenerator();
        var varName   = VariableName.TextValue(in source);

        // ── Step 1: push the RHS value onto the evaluation stack ──────────────
        if (VariableValue is not null)
        {
            // Recursively visit the RHS expression — this leaves a value on the
            // stack and sets LastEmittedType to whatever type was produced.
            ilProducer.Visit(VariableValue, in source);

            // All locals are stored as `object` in the dynamic runtime, so value
            // types (int, double, bool) must be boxed before storing. PhpValue is
            // already a reference type so this branch is rarely taken in practice —
            // but the guard keeps things correct if a future visitor emits a raw
            // value type directly.
            var valueType = ilProducer.LastEmittedType;
            if (valueType != null && valueType.IsValueType)
                generator.Emit(OpCodes.Box, valueType);
        }
        else
        {
            // No RHS — push null. In PHP `$x = null;` and a bare declaration are
            // equivalent; both result in a null local.
            generator.Emit(OpCodes.Ldnull);
        }

        // ── Step 2: resolve or allocate the local slot ────────────────────────
        int slot;
        if (!ilProducer.GetContext().TryGetVariableSlot(varName, out slot))
        {
            // First assignment to this name in the current scope — declare a new
            // IL local. All locals are typed as `object` so the runtime can hold
            // any PhpValue without a type mismatch when the same variable is
            // reassigned to a different PHP type later.
            var local = generator.DeclareLocal(typeof(object));
            slot = local.LocalIndex;

            // Register the name→slot mapping in the current frame so future reads
            // (VariableNode) and writes (VariableDeclaration) can look it up without
            // re-allocating. Registration must happen after DeclareLocal so the slot
            // index is known.
            ilProducer.GetContext().CurrentFrame.RegisterVariable(varName);
        }
        // If TryGetVariableSlot succeeded, `slot` already holds the correct index
        // and we just overwrite the existing local — standard PHP reassignment.

        // ── Step 3: pop the stack value into the local slot ───────────────────
        generator.Emit(OpCodes.Stloc, slot);

        // ── Step 4: update the emitted type tracker ───────────────────────────
        // The stored type is `object` rather than PhpValue because that's what the
        // IL local is declared as. Any subsequent read via VariableNode will emit
        // Ldloc which pushes an `object`, and callers should know to expect that.
        ilProducer.LastEmittedType = typeof(object);
    }
}