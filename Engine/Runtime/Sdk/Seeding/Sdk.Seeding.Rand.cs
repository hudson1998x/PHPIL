using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Runtime.Sdk;

public static partial class SdkInitializer
{
    public static void Rand()
    {
        FunctionTable.RegisterFunction(new PhpFunction()
        {
            Name = "rand",
            ParameterTypes = [typeof(int), typeof(int)],
            ReturnType = typeof(int),
            Method = Seeds.Rand
        });
    }
}

public static partial class Seeds
{
    private static Random _random = new Random(); 
    
    public static int Rand(int min, int max)
    {
        return _random.Next(min, max + 1);
    }
}