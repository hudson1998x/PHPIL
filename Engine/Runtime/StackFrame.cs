using System.Collections.Generic;
using System.Reflection.Emit;
using PHPIL.Engine.Runtime.Types;

namespace PHPIL.Engine.Runtime;

public class StackFrame
{
    private readonly Dictionary<string, LocalBuilder> _locals = new();
    public bool BreaksTraversal { get; set; }

    public bool TryGetVariableSlot(string name, out int slot)
    {
        if (_locals.TryGetValue(name, out var local))
        {
            slot = local.LocalIndex;
            return true;
        }
        slot = -1;
        return false;
    }

    public int RegisterVariable(string name, ILGenerator il)
    {
        if (_locals.TryGetValue(name, out var existing))
            return existing.LocalIndex;

        // Declare a PhpValue local for this variable
        var local = il.DeclareLocal(typeof(PhpValue));
        _locals[name] = local;
        return local.LocalIndex;
    }

    public int RegisterTypedVariable(string name, ILGenerator il, Type type)
    {
        if (_locals.TryGetValue(name, out var existing))
            return existing.LocalIndex;

        var local = il.DeclareLocal(type);
        _locals[name] = local;
        return local.LocalIndex;
    }
}