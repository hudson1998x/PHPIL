using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Runtime.Sdk;

public static partial class SdkInitializer
{
    static void InitConstants()
    {
        Sdk.Function("define")
            .Takes<string, object?>()
            .Returns<bool>()
            .Calls(Constants.Define);
        
        Sdk.Function("defined")
            .Takes<string>()
            .Returns<bool>()
            .Calls(Constants.Defined);
        
        Sdk.Function("constant")
            .Takes<string>()
            .Returns<object?>()
            .Calls(Constants.Constant);
    }
}

public static class Constants
{
    /// <summary>
    /// Defines a constant with the given name and value.
    /// </summary>
    public static bool Define(string name, object? value)
    {
        return ConstantTable.Define(name, value);
    }

    /// <summary>
    /// Checks if a constant is defined.
    /// </summary>
    public static bool Defined(string name)
    {
        return ConstantTable.IsDefined(name);
    }

    /// <summary>
    /// Gets the value of a constant by name.
    /// </summary>
    public static object? Constant(string name)
    {
        return ConstantTable.GetConstant(name);
    }
}
