using PHPIL.Engine.Runtime.Types;
using System;

namespace PHPIL.Engine.Runtime;

public static partial class GlobalRuntimeContext
{

    private static readonly HashSet<string> LoadedFiles = [];
    
    private static void Stdlib_ReqInc()
    {
        FunctionTable["include"] = new PhpFunction()
        {
            Name = "include",
            ReturnType = PhpValue.Void,
            Action = (params PhpValue[] items) =>
            {
                if (items.Length == 0)
                {
                    throw new InvalidOperationException("include requires argument 1 to be of type string, none given");
                }

                var arg = items[0];

                if (arg.Type != PhpType.String)
                {
                    throw new InvalidOperationException($"include requires argument 1 to be of type string, {arg.Type} given");
                }

                var path = Path.GetFullPath(arg.ToStringValue());

                if (!File.Exists(path))
                {
                    return PhpValue.Void;
                }
                
                Runtime.ExecuteFile(path);
                
                LoadedFiles.Add(path);
                
                return PhpValue.Void;
            },
            IsSystem = true,
            IsCompiled = false
        };
        FunctionTable["include"] = new PhpFunction()
        {
            Name = "include_once",
            ReturnType = PhpValue.Void,
            Action = (params PhpValue[] items) =>
            {
                if (items.Length == 0)
                {
                    throw new InvalidOperationException("include requires argument 1 to be of type string, none given");
                }

                var arg = items[0];

                if (arg.Type != PhpType.String)
                {
                    throw new InvalidOperationException($"include requires argument 1 to be of type string, {arg.Type} given");
                }

                var path = Path.GetFullPath(arg.ToStringValue());

                if (LoadedFiles.Contains(path))
                {
                    return PhpValue.Void;
                }

                if (!File.Exists(path))
                {
                    return PhpValue.Void;
                }
                
                Runtime.ExecuteFile(path);
                
                LoadedFiles.Add(path);
                
                return PhpValue.Void;
            },
            IsSystem = true,
            IsCompiled = false
        };
        
        
        FunctionTable["require"] = new PhpFunction()
        {
            Name = "require",
            ReturnType = PhpValue.Void,
            Action = (params PhpValue[] items) =>
            {
                if (items.Length == 0)
                {
                    throw new InvalidOperationException("require requires argument 1 to be of type string, none given");
                }

                var arg = items[0];

                if (arg.Type != PhpType.String)
                {
                    throw new InvalidOperationException($"require requires argument 1 to be of type string, {arg.Type} given");
                }

                var path = Path.GetFullPath(arg.ToStringValue());

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(path);
                }
                
                Runtime.ExecuteFile(path);
                
                LoadedFiles.Add(path);
                
                return PhpValue.Void;
            },
            IsSystem = true,
            IsCompiled = false
        };
        FunctionTable["require_once"] = new PhpFunction()
        {
            Name = "require_once",
            ReturnType = PhpValue.Void,
            Action = (params PhpValue[] items) =>
            {
                if (items.Length == 0)
                {
                    throw new InvalidOperationException("require_once requires argument 1 to be of type string, none given");
                }

                var arg = items[0];

                if (arg.Type != PhpType.String)
                {
                    throw new InvalidOperationException($"require_once requires argument 1 to be of type string, {arg.Type} given");
                }

                var path = Path.GetFullPath(arg.ToStringValue());

                if (LoadedFiles.Contains(path))
                {
                    return PhpValue.Void;
                }

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(path);
                }
                
                Runtime.ExecuteFile(path);
                
                LoadedFiles.Add(path);
                
                return PhpValue.Void;
            },
            IsSystem = true,
            IsCompiled = false
        };
    }
}