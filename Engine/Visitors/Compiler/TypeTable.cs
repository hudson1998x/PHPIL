using System.Collections.Generic;
using System.Reflection;

namespace PHPIL.Engine.Visitors;

/// <summary>
/// Global registry of compiled PHP types and their methods, used to resolve class
/// definitions and method handles during compilation and runtime dispatch.
/// </summary>
public static class TypeTable
{
    /// <summary>
    /// The backing store mapping fully-qualified PHP type names to their <see cref="PhpType"/> descriptors.
    /// </summary>
    private static readonly Dictionary<string, PhpType> Types = [];

    /// <summary>
    /// Maps <c>(typeName, methodName)</c> pairs to their compiled <see cref="MethodInfo"/> handles,
    /// populated by <see cref="Compiler.VisitClassNode"/> after each method body is emitted.
    /// </summary>
    private static readonly Dictionary<(string typeName, string methodName), MethodInfo> Methods = [];

    /// <summary>
    /// Registers or replaces a PHP type in the table under its fully-qualified name.
    /// </summary>
    /// <param name="type">The <see cref="PhpType"/> to register.</param>
    public static void RegisterType(PhpType type)
    {
        Types[type.Name] = type;
    }

    /// <summary>
    /// Registers or replaces a method handle for a given type and method name.
    /// </summary>
    /// <param name="typeName">The fully-qualified PHP type name.</param>
    /// <param name="methodName">The method name as declared in PHP source.</param>
    /// <param name="method">The <see cref="MethodInfo"/> handle to register.</param>
    public static void RegisterMethod(string typeName, string methodName, MethodInfo method)
    {
        Methods[(typeName, methodName)] = method;
    }

    /// <summary>
    /// Looks up a method handle by type and method name.
    /// </summary>
    /// <param name="typeName">The fully-qualified PHP type name.</param>
    /// <param name="methodName">The method name to look up.</param>
    /// <returns>
    /// The registered <see cref="MethodInfo"/>, or <see langword="null"/> if no match is found.
    /// </returns>
    public static MethodInfo? GetMethod(string typeName, string methodName)
    {
        if (Methods.TryGetValue((typeName, methodName), out var method)) return method;
        return null;
    }

    /// <summary>
    /// Looks up a PHP type by its fully-qualified name, triggering autoloading if the type
    /// is not yet registered.
    /// </summary>
    /// <param name="name">The fully-qualified PHP type name to look up.</param>
    /// <returns>
    /// The <see cref="PhpType"/> registered under <paramref name="name"/>, or
    /// <see langword="null"/> if the type is not found and autoloading does not register it.
    /// </returns>
    /// <remarks>
    /// <c>self</c> is never passed to the autoloader — it must be resolved to a concrete type
    /// by the caller before invoking this method.
    /// </remarks>
    public static PhpType? GetType(string name)
    {
        if (Types.TryGetValue(name, out var type)) return type;
        
        // Don't try to autoload "self" - it should be resolved by context
        if (name.Equals("self", StringComparison.OrdinalIgnoreCase))
            return null;
        
        // Try autoload
        if (Runtime.Runtime.Autoload(name))
        {
            if (Types.TryGetValue(name, out type)) return type;
        }
        return null;
    }

    /// <summary>
    /// Performs a reverse lookup to find a <see cref="PhpType"/> by its finished CLR
    /// <see cref="Type"/>.
    /// </summary>
    /// <param name="runtimeType">The CLR <see cref="Type"/> to search for.</param>
    /// <returns>
    /// The <see cref="PhpType"/> whose <c>RuntimeType</c> matches <paramref name="runtimeType"/>,
    /// or <see langword="null"/> if no match is found.
    /// </returns>
    /// <remarks>
    /// This is a linear scan over all registered types. It is intended for occasional use
    /// (e.g. resolving <c>parent</c> from a known CLR type) rather than hot paths.
    /// </remarks>
    public static PhpType? GetTypeByRuntime(Type runtimeType)
    {
        foreach (var type in Types.Values)
        {
            if (type.RuntimeType == runtimeType)
                return type;
        }
        return null;
    }

    /// <summary>
    /// Clears all registered types and methods.
    /// </summary>
    /// <remarks>
    /// Intended for use between interpreter resets or test runs where a clean type namespace
    /// is required. Should be called alongside <see cref="FunctionTable.Reset"/> and
    /// <see cref="Compiler.ResetModule"/>.
    /// </remarks>
    public static void Clear()
    {
        Types.Clear();
        Methods.Clear();
    }
}