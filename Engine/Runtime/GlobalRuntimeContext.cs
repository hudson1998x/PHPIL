using PHPIL.Engine.Runtime.Types;

namespace PHPIL.Engine.Runtime;

public static partial class GlobalRuntimeContext
{
    /// <summary>
    /// Where ALL functions sit.
    /// </summary>
    public static readonly Dictionary<string, PhpFunction> FunctionTable = [];
    
    public static readonly MemoryStream StdoutStream = new();
    public static readonly StreamWriter Stdout = new(StdoutStream) { AutoFlush = true };

    static GlobalRuntimeContext()
    {
        Stdlib_Dev();
        Stdlib_ReqInc();
    }
    
}