using PHPIL.Engine.Runtime;

namespace PHPIL.Engine.Runtime.Sdk;

public static partial class SdkInitializer
{
    static void InitAutoload()
    {
        Sdk.Function("spl_autoload_register")
            .Takes<object?>() // Can be null, a Closure, or a callable array
            .Returns<bool>()
            .Calls(Autoload.Register);
    }
}

public static class Autoload
{
    public static bool Register(object? callable)
    {
        if (callable == null)
        {
            Runtime.RegisterAutoloader(new Action<string>(className =>
            {
            }));
            return true;
        }

        if (callable is Closure closure)
        {
            Runtime.RegisterAutoloader(new Action<string>(className =>
            {
                closure.Invoke(className);
            }));
            return true;
        }

        return false;
    }
}
