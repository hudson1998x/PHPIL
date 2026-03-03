using System.Collections.Generic;
using System.Reflection.Emit;
using PHPIL.Engine.Runtime.Types;

namespace PHPIL.Engine.Runtime;

public class StackFrame
{
    private readonly Dictionary<string, LocalBuilder> _variables = new();

    public bool BreaksTraversal { get; set; } = false;

    public int RegisterVariable(string name, ILGenerator il)
    {
        if (_variables.TryGetValue(name, out var existing))
            return existing.LocalIndex;

        // Declare the local as a PhpValue so the JIT is happy
        var local = il.DeclareLocal(typeof(PhpValue));
        _variables[name] = local;
        return local.LocalIndex;
    }

    // Overload for when you already have the index (from VariableDeclaration logic)
    public void RegisterVariable(string name, LocalBuilder local)
    {
        _variables[name] = local;
    }

    public bool TryGetVariableSlot(string name, out int slot)
    {
        if (_variables.TryGetValue(name, out var local))
        {
            slot = local.LocalIndex;
            return true;
        }
        slot = -1;
        return false;
    }
}