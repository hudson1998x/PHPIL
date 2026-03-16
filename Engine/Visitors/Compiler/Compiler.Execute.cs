using System.Reflection.Emit;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Finalises the compiled method by emitting a <see cref="OpCodes.Ret"/> instruction and
    /// immediately invoking it.
    /// </summary>
    /// <exception cref="Exception">
    /// Any exception thrown during invocation is logged to <see cref="Console.Out"/> and
    /// re-thrown to the caller.
    /// </exception>
    /// <remarks>
    /// This method is intended for top-level script execution where the compiled
    /// <see cref="System.Reflection.Emit.DynamicMethod"/> represents the program entry point.
    /// It should only be called once all statements have been visited and the IL stream is
    /// complete. Invoking it on a compiler constructed from an existing
    /// <see cref="ILGenerator"/> (i.e. where <c>_method</c> is <see langword="null"/>) will
    /// throw a <see cref="NullReferenceException"/>.
    /// </remarks>
    public void Execute()
    {
        Emit(OpCodes.Ret);
        try
        {
            _method.Invoke(null, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION during execution: {ex}");
            throw;
        }
    }
}