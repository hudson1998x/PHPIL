using PHPIL.Engine.Runtime.Types;

namespace PHPIL.Engine.Runtime;

public static partial class GlobalRuntimeContext
{
    public static PhpValue CallFunction(string name, PhpValue[] args)
    {
        if (!FunctionTable.TryGetValue(name, out var fn))
            throw new InvalidOperationException($"Call to undefined function {name}()");

        if (fn.Action == null)
            throw new InvalidOperationException($"Function {name}() has no implementation");

        return fn.Action(args);
    }
}