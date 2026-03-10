using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Runtime.Sdk;

public static partial class SdkInitializer
{
    static void Print()
    {
        FunctionTable.RegisterFunction(new PhpFunction()
        {
            Name = "print",
            ParameterTypes = [typeof(string)],
            Method = Streams.Print
        });
    }
}

public static partial class Streams
{
    public static void Print(string value) => Console.WriteLine(value);
}