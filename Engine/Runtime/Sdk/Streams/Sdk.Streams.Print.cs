using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Runtime.Sdk;

public static partial class SdkInitializer
{
    internal static readonly MemoryStream StdoutMemory = new();
    internal static readonly StreamWriter StdoutStream = new(StdoutMemory);
    
    static void Print()
    {
        FunctionTable.RegisterFunction(new PhpFunction()
        {
            Name = "print",
            ParameterTypes = [typeof(string)],
            MethodInfo = typeof(Streams).GetMethod(nameof(Streams.Print)),
            Method = Streams.Print
        });
    }
}

public static partial class Streams
{
    // The .Replace must be replaced for something better. This is a temporary solution.
    public static void Print(string value) => SdkInitializer.StdoutStream.Write(value.Replace("\\n", "\n"));
}