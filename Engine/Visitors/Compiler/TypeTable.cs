using System.Collections.Generic;
using System.Reflection;

namespace PHPIL.Engine.Visitors;

public static class TypeTable
{
    private static readonly Dictionary<string, PhpType> Types = [];
    private static readonly Dictionary<(string typeName, string methodName), MethodInfo> Methods = [];

    public static void RegisterType(PhpType type)
    {
        Types[type.Name] = type;
    }

    public static void RegisterMethod(string typeName, string methodName, MethodInfo method)
    {
        Methods[(typeName, methodName)] = method;
    }

    public static MethodInfo? GetMethod(string typeName, string methodName)
    {
        if (Methods.TryGetValue((typeName, methodName), out var method)) return method;
        return null;
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