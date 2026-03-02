using PHPIL.Engine.Runtime.Types;

namespace PHPIL.Engine.Runtime;

public class StackFrame
{
    private readonly Dictionary<string, int> _variables = new();
    private int _nextSlot = 0;

    public bool BreaksTraversal { get; set; } = false;

    public int RegisterVariable(string name)
    {
        if (_variables.TryGetValue(name, out var existing))
            return existing;

        var slot = _nextSlot++;
        _variables[name] = slot;
        return slot;
    }

    public void RegisterVariable(string name, int slot)
        => _variables[name] = slot;

    public bool TryGetVariableSlot(string name, out int slot)
        => _variables.TryGetValue(name, out slot);

    public IReadOnlyDictionary<string, int> Variables => _variables;
}