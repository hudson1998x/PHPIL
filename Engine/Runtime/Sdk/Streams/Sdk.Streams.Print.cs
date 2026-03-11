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
            Method = Streams.Print
        });
    }
}

public static partial class Streams
{
    public static void Print(string value) => SdkInitializer.StdoutStream.Write(value);
}