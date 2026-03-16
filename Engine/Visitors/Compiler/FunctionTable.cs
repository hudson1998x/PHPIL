using System;
using System.Collections.Generic;

namespace PHPIL.Engine.Visitors
{
    /// <summary>
    /// Global registry of compiled PHP functions, mapping fully-qualified names to their
    /// <see cref="PhpFunction"/> descriptors.
    /// </summary>
    public static class FunctionTable
    {
        /// <summary>
        /// The backing store mapping fully-qualified function names to their descriptors.
        /// </summary>
        private static readonly Dictionary<string, PhpFunction> Functions = [];

        /// <summary>
        /// Monotonically increasing counter used to generate unique anonymous function names.
        /// </summary>
        private static int _anonymousId = 0;

        /// <summary>
        /// Registers or replaces a function in the table under its fully-qualified name.
        /// </summary>
        /// <param name="function">The <see cref="PhpFunction"/> to register.</param>
        public static void RegisterFunction(PhpFunction function)
        {
            Functions[function.Name!] = function;
        }

        /// <summary>
        /// Returns a unique string identifier for the next anonymous function and advances
        /// the internal counter.
        /// </summary>
        /// <returns>A string representation of the current anonymous function index.</returns>
        public static string GetNextAnonymousId()
        {
            return (_anonymousId++).ToString();
        }

        /// <summary>
        /// Looks up a function by its fully-qualified name.
        /// </summary>
        /// <param name="name">The fully-qualified function name to look up.</param>
        /// <returns>
        /// The <see cref="PhpFunction"/> registered under <paramref name="name"/>, or
        /// <see langword="null"/> if no such function has been registered.
        /// </returns>
        public static PhpFunction? GetFunction(string name)
        {
            return Functions.TryGetValue(name, out PhpFunction? function) ? function : null;
        }

        /// <summary>
        /// Clears all registered functions and resets the anonymous function counter.
        /// </summary>
        /// <remarks>
        /// Intended for use between interpreter resets or test runs where a clean function
        /// namespace is required.
        /// </remarks>
        public static void Reset()
        {
            Functions.Clear();
            _anonymousId = 0;
        }
    }
}