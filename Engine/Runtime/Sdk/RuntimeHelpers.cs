using System.Reflection;
using System.Reflection.Emit;

namespace PHPIL.Engine.Runtime.Sdk;

public static class RuntimeHelpers
{
    public static object CallMethod(object? obj, string methodName, object?[] args)
    {
        if (obj == null) throw new Exception("Cannot call method on null");
        
        var type = obj.GetType();
        
        try 
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == args.Length)
                {
                    return method.Invoke(obj, args) ?? (object)"";
                }
            }
            
            if (methods.Length > 0)
            {
                var paramCounts = string.Join(", ", methods.Select(m => m.GetParameters().Length));
                throw new Exception($"Method '{methodName}' found but no matching overload ({paramCounts})");
            }
        }
        catch (System.Reflection.TargetInvocationException)
        {
            // Swallow - this happens with dynamic types
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error calling method '{methodName}' on {type.Name}: {ex.Message}");
        }
        
        throw new Exception($"Method '{methodName}' not found on type '{type.Name}'");
    }

    public static object CallStaticMethod(Type type, string methodName, object?[] args)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == args.Length)
            {
                return method.Invoke(null, args) ?? (object)"";
            }
        }
        
        if (methods.Length > 0)
        {
            var paramCounts = string.Join(", ", methods.Select(m => m.GetParameters().Length));
            throw new Exception($"Static method '{methodName}' found but no matching overload ({paramCounts})");
        }
        
        throw new Exception($"Static method '{methodName}' not found on type '{type.Name}'");
    }

    public static object? GetProperty(object? obj, string propertyName)
    {
        if (obj == null) throw new Exception("Cannot access property on null");
        
        var type = obj.GetType();
        
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
        
        if (field != null)
        {
            var value = field.GetValue(obj);
            return value?.ToString() ?? "";
        }
        
        var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            var value = prop.GetValue(obj);
            return value?.ToString() ?? "";
        }
        
        throw new Exception($"Property '{propertyName}' not found on type '{type.Name}'");
    }

    public static void SetProperty(object? obj, string propertyName, object? value)
    {
        if (obj == null) throw new Exception("Cannot set property on null");
        
        var type = obj.GetType();
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
            return;
        }
        
        var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            prop.SetValue(obj, value);
            return;
        }
        
        throw new Exception($"Property '{propertyName}' not found on type '{type.Name}'");
    }

    public static bool InstanceOf(object? obj, string className)
    {
        if (obj == null) return false;
        
        var type = obj.GetType();
        var phpType = Visitors.TypeTable.GetType(className);
        
        if (phpType?.RuntimeType != null)
        {
            return phpType.RuntimeType.IsAssignableFrom(type);
        }
        
        return type.Name == className || type.FullName == className;
    }

    public static void TryCallConstruct(object? obj, object?[] args)
    {
        if (obj == null) return;
        
        var type = obj.GetType();
        
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            .Where(m => m.Name.Equals("__construct", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == args.Length)
            {
                method.Invoke(obj, args);
                return;
            }
        }
    }
    
    private static readonly Dictionary<(string Type, string Property), object?> _staticPropertyDefaults = new();
    
    public static void SetStaticPropertyDefault(string typeName, string propertyName, object? value)
    {
        _staticPropertyDefaults[(typeName, propertyName)] = value;
    }
    
    public static object? GetStaticProperty(string propertyName, string typeName)
    {
        // Check if we have a stored default value
        if (_staticPropertyDefaults.TryGetValue((typeName, propertyName), out var defaultValue))
        {
            return defaultValue?.ToString() ?? "";
        }
        
        // Try to get from the actual type
        var phpType = Visitors.TypeTable.GetType(typeName);
        if (phpType?.RuntimeType == null) throw new Exception($"Type '{typeName}' not found.");
        
        var type = phpType.RuntimeType;
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Static);
        if (field != null)
        {
            try
            {
                var value = field.GetValue(null);
                return value?.ToString() ?? "";
            }
            catch
            {
                // Field value not set yet, return empty
                return "";
            }
        }
        
        throw new Exception($"Static property '{propertyName}' not found on type '{typeName}'");
    }
}
