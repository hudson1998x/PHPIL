using System;
using System.Collections.Generic;

namespace PHPIL.Engine.Visitors
{
    public static class FunctionTable
    {
        private static readonly Dictionary<string, PhpFunction> Functions = [];
        private static int _anonymousId = 0;

        public static void RegisterFunction(PhpFunction function)
        {
            Functions[function.Name!] = function;
        }

        public static string GetNextAnonymousId()
        {
            return (_anonymousId++).ToString();
        }

        public static PhpFunction? GetFunction(string name)
        {
            return Functions.TryGetValue(name, out PhpFunction? function) ? function : null;
        }

        public static void Reset()
        {
            Functions.Clear();
            _anonymousId = 0;
        }
    }
}
