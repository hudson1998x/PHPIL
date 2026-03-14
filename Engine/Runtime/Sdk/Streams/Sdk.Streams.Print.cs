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
            ParameterTypes = [typeof(object)],
            MethodInfo = typeof(Streams).GetMethod(nameof(Streams.Print)),
            Method = Streams.Print
        });
    }
}

public static partial class Streams
{
    public static void Print(object value)
    {
        string str = value switch
        {
            bool b => b ? "1" : "",
            null => "",
            _ => value.ToString()?.Replace("\\n", "\n") ?? ""
        };
        SdkInitializer.StdoutStream.Write(str);
    }
}