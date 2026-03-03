using PHPIL.Engine.Runtime.Types;
using System;

namespace PHPIL.Engine.Runtime;

public static partial class GlobalRuntimeContext
{
    private static void Stdlib_Dev()
    {
        FunctionTable["var_dump"] = new PhpFunction()
        {
            Name = "var_dump",
            IsSystem = true,
            ReturnType = PhpValue.Void,
            Action = (params PhpValue[] items) =>
            {
                foreach (var item in items)
                {
                    // Use the PhpValue.Type enum for the type name
                    var typeName = item.Type.ToString();

                    // Use DebugInfo() to get value representation
                    

                    Stdout.Write($"{typeName}: {item.ToStringValue()}");
                }

                // var_dump in PHP does not return a value, so return Void
                return PhpValue.Void;
            },
            IsCompiled = false
        };

        FunctionTable["print"] = new PhpFunction()
        {
            Name = "print",
            IsSystem = true,
            ReturnType = PhpValue.Void,
            Action = (params PhpValue[] items) =>
            {
                foreach (var item in items)
                {
                    Stdout.Write(item.ToStringValue());
                }

                return PhpValue.Void;
            },
            IsCompiled = false
        };
    }
}