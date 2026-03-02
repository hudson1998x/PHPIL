using System.Reflection.Emit;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.IlProducer;

namespace PHPIL.Engine.SyntaxTree;

/// <summary>
/// Visitor implementation for <see cref="VariableNode"/> — emits IL that loads
/// the value of a previously declared variable onto the evaluation stack.
/// </summary>
public partial class VariableNode
{
    public override void Accept(IVisitor visitor, in ReadOnlySpan<char> source)
    {
        if (visitor is not IlProducer ilProducer) return;
        var il = ilProducer.GetILGenerator();

        // Look up the variable's local slot index by name in the current scope stack.
        // All locals are stored as PhpValue, so no coercion or boxing is needed after
        // the load — whatever is consuming this node can use the value directly.
        if (ilProducer.GetContext().TryGetVariableSlot(Token.TextValue(in source), out var localIndex))
        {
            il.Emit(OpCodes.Ldloc, localIndex); // push the PhpValue local onto the stack
        }
        else
        {
            // PHP normally allows reading an undefined variable (yielding null with a notice),
            // but throwing here enforces stricter semantics during compilation — undefined
            // variable reads are surfaced as hard errors rather than silent nulls.
            // This can be relaxed later if PHP's lenient behaviour is required.
            throw new Exception("Variable not found");
        }

        // Signal to the parent node that a PhpValue is now on top of the stack.
        ilProducer.LastEmittedType = typeof(PhpValue);
    }
}