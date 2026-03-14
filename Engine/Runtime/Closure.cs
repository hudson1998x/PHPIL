using System;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Runtime
{
    public class Closure
    {
        private readonly string _functionName;

        public Closure(string functionName)
        {
            _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
        }

        public object? Invoke(params object?[] args)
        {
            var func = FunctionTable.GetFunction(_functionName);
            if (func?.Method == null)
            {
                throw new InvalidOperationException($"Closure function '{_functionName}' not found");
            }
            
            if (args.Length == 1)
            {
                return func.Method.DynamicInvoke(args[0]);
            }
            else if (args.Length == 0)
            {
                return func.Method.DynamicInvoke(null);
            }
            else
            {
                return func.Method.DynamicInvoke(args);
            }
        }
    }
}