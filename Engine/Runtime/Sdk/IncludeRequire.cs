using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Runtime.Sdk;

public static partial class SdkInitializer
{
    static void InitIncludeRequire()
    {
        RequireOnce();
    }
}
public static partial class SdkInitializer
{
    static void RequireOnce()
    {
        FunctionTable.RegisterFunction(new PhpFunction()
        {
            Name = "require_once",
            ParameterTypes = [typeof(string)],
            MethodInfo = typeof(IncludeRequire).GetMethod(nameof(IncludeRequire.RequireOnce)),
            Method = IncludeRequire.RequireOnce
        });
    }
}

public static partial class IncludeRequire
{
    public static void RequireOnce(string value)
    {
        var context = Runtime.CurrentContext;
        var filePath = Path.GetFullPath(value);

        if (context != null)
        {
            // Check if file has already been required in this execution context
            if (!context.MarkFileRequired(filePath))
            {
                // File was already required, skip
                return;
            }
        }

        // File hasn't been required yet (or no context), execute it
        Runtime.ExecuteFile(value);
    }
}