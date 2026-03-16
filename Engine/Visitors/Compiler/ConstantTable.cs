using System.Collections.Generic;

namespace PHPIL.Engine.Visitors;

/// <summary>
/// Global registry of user-defined PHP constants, mapping names to their values.
/// Constants defined via define() are stored here and can be accessed across all executions.
/// </summary>
public static class ConstantTable
{
    /// <summary>
    /// The backing store mapping constant names to their values.
    /// </summary>
    private static readonly Dictionary<string, object?> _constants = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Lock for thread-safe access.
    /// </summary>
    private static readonly object _lock = new();

    /// <summary>
    /// Defines a new constant or overwrites an existing one.
    /// </summary>
    /// <param name="name">The constant name (case-insensitive).</param>
    /// <param name="value">The constant value.</param>
    /// <returns>True if the constant was newly defined, false if it already existed and was overwritten.</returns>
    public static bool Define(string name, object? value)
    {
        lock (_lock)
        {
            bool isNew = !_constants.ContainsKey(name);
            _constants[name] = value;
            return isNew;
        }
    }

    /// <summary>
    /// Gets a constant value by name.
    /// </summary>
    /// <param name="name">The constant name (case-insensitive).</param>
    /// <returns>The constant value, or null if not found.</returns>
    public static object? GetConstant(string name)
    {
        lock (_lock)
        {
            if (_constants.TryGetValue(name, out var value))
                return value;
            return null;
        }
    }

    /// <summary>
    /// Checks if a constant is defined.
    /// </summary>
    /// <param name="name">The constant name (case-insensitive).</param>
    /// <returns>True if the constant is defined.</returns>
    public static bool IsDefined(string name)
    {
        lock (_lock)
        {
            return _constants.ContainsKey(name);
        }
    }

    /// <summary>
    /// Clears all user-defined constants. Used for testing/reset.
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _constants.Clear();
        }
    }
}
