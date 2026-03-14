using System;
using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Runtime
{
    /// <summary>
    /// Represents a callable closure (anonymous function).
    /// </summary>
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
            return func.Method.DynamicInvoke(args);
        }
    }
}
