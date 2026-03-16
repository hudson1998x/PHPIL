using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.Exceptions;

namespace PHPIL.Engine.Runtime.Sdk;

public static class RuntimeHelpers
{
    public static object CallMethod(object? obj, string methodName, object?[] args)
    {
        // Expand spread arguments
        var finalArgs = new List<object?>();
        foreach (var arg in args)
        {
            if (arg is System.Collections.Generic.Dictionary<object, object> dictionary)
            {
                foreach (var value in dictionary.Values)
                {
                    finalArgs.Add(value);
                }
            }
            else if (arg is System.Collections.IEnumerable enumerable && arg is not string)
            {
                foreach (var item in enumerable)
                {
                    finalArgs.Add(item);
                }
            }
            else
            {
                finalArgs.Add(arg);
            }
        }
        
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
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(object[]))
                {
                    // Variadic method - pack args into array
                    object[] packedArgs = new object[] { finalArgs.ToArray() };
                    return method.Invoke(obj, packedArgs) ?? (object)"";
                }
                else if (parameters.Length == finalArgs.Count)
                {
                    return method.Invoke(obj, finalArgs.ToArray()) ?? (object)"";
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
        // Expand spread arguments
        var finalArgs = new List<object?>();
        foreach (var arg in args)
        {
            if (arg is System.Collections.Generic.Dictionary<object, object> dictionary)
            {
                foreach (var value in dictionary.Values)
                {
                    finalArgs.Add(value);
                }
            }
            else if (arg is System.Collections.IEnumerable enumerable && arg is not string)
            {
                foreach (var item in enumerable)
                {
                    finalArgs.Add(item);
                }
            }
            else
            {
                finalArgs.Add(arg);
            }
        }
        
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(object[]))
            {
                // Variadic method - pack args into array
                object[] packedArgs = new object[] { finalArgs.ToArray() };
                return method.Invoke(null, packedArgs) ?? (object)"";
            }
            else if (parameters.Length == finalArgs.Count)
            {
                return method.Invoke(null, finalArgs.ToArray()) ?? (object)"";
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

    public static object ResolveAndCreate(string className)
    {
        var phpType = Visitors.TypeTable.GetType(className);
        if (phpType?.RuntimeType == null)
            throw new Exception($"Class '{className}' not found.");
        var constructor = phpType.RuntimeType.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            throw new Exception($"No parameterless constructor found for '{className}'.");
        return constructor.Invoke(null);
    }
    
    private static readonly Dictionary<(string Type, string Property), object?> _staticPropertyDefaults = new();
    
    public static void SetStaticPropertyDefault(string typeName, string propertyName, object? value)
    {
        _staticPropertyDefaults[(typeName, propertyName)] = value;
    }
    
    public static object? GetStaticProperty(string propertyName, string typeName)
    {
        // Normalize typeName - convert dots to backslashes to match how defaults are stored
        var normalizedTypeName = typeName.Replace(".", "\\");
        
        // Check if we have a stored default value
        if (_staticPropertyDefaults.TryGetValue((normalizedTypeName, propertyName), out var defaultValue))
        {
            return defaultValue;
        }
        
        // Try to get from the actual type
        var phpType = Visitors.TypeTable.GetType(normalizedTypeName);
        if (phpType?.RuntimeType == null) 
        {
            throw new Exception($"Type '{typeName}' not found.");
        }
        
        var type = phpType.RuntimeType;
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        if (field != null)
        {
            try
            {
                return field.GetValue(null);
            }
            catch
            {
                // Field value not set yet, return null
                return null;
            }
        }
        
        throw new Exception($"Static property '{propertyName}' not found on type '{typeName}'");
    }
    
    public static void SetStaticField(Type type, string fieldName, object? value)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(null, value);
            return;
        }
        
        throw new Exception($"Static field '{fieldName}' not found on type '{type.Name}'");
    }
    
    public static void SetStaticFieldByName(string typeName, string fieldName, object? value)
    {
        var normalizedTypeName = typeName.Replace(".", "\\");
        
        var phpType = Visitors.TypeTable.GetType(normalizedTypeName);
        if (phpType?.RuntimeType == null) 
        {
            throw new Exception($"Type '{typeName}' not found.");
        }
        
        var field = phpType.RuntimeType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(null, value);
            return;
        }
        
        throw new Exception($"Static field '{fieldName}' not found on type '{typeName}'");
    }

    public static object CallStaticMethodByName(string typeName, string methodName, object?[] args)
    {
        // Expand spread arguments
        var finalArgs = new List<object?>();
        foreach (var arg in args)
        {
            if (arg is System.Collections.Generic.Dictionary<object, object> dictionary)
            {
                foreach (var value in dictionary.Values)
                {
                    finalArgs.Add(value);
                }
            }
            else if (arg is System.Collections.IEnumerable enumerable && arg is not string)
            {
                foreach (var item in enumerable)
                {
                    finalArgs.Add(item);
                }
            }
            else
            {
                finalArgs.Add(arg);
            }
        }

        var normalizedTypeName = typeName.Replace(".", "\\");
        var phpType = Visitors.TypeTable.GetType(normalizedTypeName);
        if (phpType?.RuntimeType == null)
        {
            throw new Exception($"Type '{typeName}' not found.");
        }

        var type = phpType.RuntimeType;
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == finalArgs.Count)
            {
                return method.Invoke(null, finalArgs.ToArray()) ?? (object)"";
            }
        }

        if (methods.Length > 0)
        {
            var paramCounts = string.Join(", ", methods.Select(m => m.GetParameters().Length));
            throw new Exception($"Static method '{methodName}' found but no matching overload ({paramCounts})");
        }

        throw new Exception($"Static method '{methodName}' not found on type '{typeName}'");
    }

    public static object CallVariadicFunction(string functionName, object?[] args)
    {
        // For variadic functions, args contains the arguments to be packed into an array.
        // The function expects a single object[] parameter.
        
        // Look up the function in the function table
        var phpFunc = Visitors.FunctionTable.GetFunction(functionName);
        if (phpFunc == null)
        {
            throw new Exception($"Function '{functionName}' not found");
        }
        
        var methodToCall = phpFunc.MethodInfo ?? phpFunc.Method?.Method;
        if (methodToCall == null)
        {
            throw new Exception($"Cannot call function '{functionName}': no method available");
        }
        
        // Pack args into a single array
        object[] packedArgs = new object[] { args };
        
        // Invoke the function with packed args
        var result = methodToCall.Invoke(null, packedArgs);
        return result ?? "";
    }

    public static object CallVariadicMethod(object? obj, string methodName, object?[] args)
    {
        // For variadic methods, args contains the arguments to be packed into an array.
        // The method expects a single object[] parameter.
        
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
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(object[]))
                {
                    // Pack args into a single array
                    object[] packedArgs = new object[] { args };
                    return method.Invoke(obj, packedArgs) ?? (object)"";
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
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error calling method '{methodName}' on {type.Name}: {ex.Message}");
        }
        
        throw new Exception($"Method '{methodName}' not found on type '{type.Name}'");
    }

    public static object CallStaticVariadicMethod(Type type, string methodName, object?[] args)
    {
        // For variadic static methods, args contains the arguments to be packed into an array.
        // The method expects a single object[] parameter.
        
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(object[]))
            {
                // Pack args into a single array
                object[] packedArgs = new object[] { args };
                return method.Invoke(null, packedArgs) ?? (object)"";
            }
        }
        
        if (methods.Length > 0)
        {
            var paramCounts = string.Join(", ", methods.Select(m => m.GetParameters().Length));
            throw new Exception($"Static method '{methodName}' found but no matching overload ({paramCounts})");
        }
        
        throw new Exception($"Static method '{methodName}' not found on type '{type.Name}'");
    }

    public static object CallFunctionWithSpread(string functionName, object?[] args)
    {
        // For PHP functions, we need to handle spread arguments
        // The args array may contain arrays (from spread) that need to be expanded
        
        // Collect all arguments, expanding any arrays
        var finalArgs = new List<object?>();
        
        foreach (var arg in args)
        {
            if (arg is System.Collections.Generic.Dictionary<object, object> dictionary)
            {
                // It's a PHP array (dictionary), expand its VALUES
                // PHP arrays are ordered maps, but Dictionary iteration order is not guaranteed
                // However, we rely on .NET Core's Dictionary preserving insertion order
                foreach (var value in dictionary.Values)
                {
                    finalArgs.Add(value);
                }
            }
            else if (arg is System.Collections.IEnumerable enumerable && arg is not string)
            {
                // It's some other collection, expand it
                foreach (var item in enumerable)
                {
                    finalArgs.Add(item);
                }
            }
            else
            {
                finalArgs.Add(arg);
            }
        }
        
        // Look up the function in the function table
        var phpFunc = Visitors.FunctionTable.GetFunction(functionName);
        if (phpFunc == null)
        {
            throw new FunctionNotDefinedException(functionName, "TODO(RuntimeHelpers.cs, L495)", 0, 0);
        }
        
        var methodToCall = phpFunc.MethodInfo ?? phpFunc.Method?.Method;
        if (methodToCall == null)
        {
            throw new Exception($"Cannot call function '{functionName}': no method available");
        }
        
        // Debug: check parameter count
        var parameters = methodToCall.GetParameters();
        if (parameters.Length != finalArgs.Count)
        {
            throw new Exception($"Parameter count mismatch for {functionName}: expected {parameters.Length}, got {finalArgs.Count}. Args: {string.Join(", ", args.Select(a => a?.GetType().Name ?? "null"))}");
        }

        // Invoke the function with expanded arguments
        var result = methodToCall.Invoke(null, finalArgs.ToArray());
        return result ?? "";
    }

    public static object? GetArrayElementForIsset(object? array, object? key)
    {
        // Safely get an array element for isset checking
        // Returns null if array is null, key doesn't exist, or value is null
        if (array == null)
            return null;

        if (array is System.Collections.Generic.Dictionary<object, object> dict)
        {
            if (dict.TryGetValue(key!, out var value))
                return value;
            return null;
        }

        // For other collection types, try using dynamic access
        try
        {
            dynamic d = array;
            return d[key];
        }
        catch
        {
            return null;
        }
    }

    public static bool IssetHelper(object?[] values)
    {
        // isset() returns true if all arguments are set and not null
        // In PHP:
        // - isset($var) returns false if $var is not set or is null
        // - isset($var1, $var2, ...) returns true only if ALL are set and not null
        // - isset($array[$key]) returns false if array doesn't exist or key doesn't exist
        // - isset($object->property) returns false if object or property doesn't exist
        
        if (values == null || values.Length == 0)
            return false;

        foreach (var value in values)
        {
            // Check if the value is null
            if (value == null)
                return false;
        }

        // All values are set and not null
        return true;
    }
}
