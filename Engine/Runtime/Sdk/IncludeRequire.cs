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
        Runtime.ExecuteFile(value);
    }
}