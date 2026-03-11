using System.Collections.Generic;

namespace PHPIL.Engine.Visitors;

public static class TypeTable
{
    private static readonly Dictionary<string, PhpType> Types = [];

    public static void RegisterType(PhpType type)
    {
        Types[type.Name] = type;
    }

    public static PhpType? GetType(string name)
    {
        if (Types.TryGetValue(name, out var type)) return type;
        
        // Try autoload
        if (Runtime.Runtime.Autoload(name))
        {
            if (Types.TryGetValue(name, out type)) return type;
        }
        return null;
    }
}