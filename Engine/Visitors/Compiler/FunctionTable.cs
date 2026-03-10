using System.Reflection.Emit;

namespace PHPIL.Engine.Visitors;

public static class FunctionTable
{
    private static readonly Dictionary<string, PhpFunction> Functions = [];

    public static void RegisterFunction(PhpFunction function)
    {
        Functions.Add(function.Name!, function);
    }

    public static PhpFunction? GetFunction(string name)
    {
        return Functions.TryGetValue(name, out PhpFunction? function) ? function: null;
    }
}