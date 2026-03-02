using PHPIL.Engine.Runtime.Types;
using System;

namespace PHPIL.Engine.Runtime;

public static partial class GlobalRuntimeContext
{
    private static void Stdlib_Strings()
    {
        FunctionTable["strlen"] = new PhpFunction()
        {
            Name = "strlen",
            IsSystem = true,
            ReturnType = new PhpValue(0),
            Action = (params PhpValue[] items) =>
            {
                if (items.Length != 1)
                {
                    throw new Exception("Expected exactly 1 argument");
                }
                
                return new PhpValue(items[0].ToString().Length);
            },
            IsCompiled = false
        };
    }
}