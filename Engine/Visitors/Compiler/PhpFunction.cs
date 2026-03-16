namespace PHPIL.Engine.Visitors;

/// <summary>
/// Describes a compiled PHP function, holding the metadata and invocation handle needed
/// by the <see cref="Compiler"/> to emit call sites and by <see cref="FunctionTable"/> to
/// register and resolve functions at runtime.
/// </summary>
public class PhpFunction
{
    /// <summary>
    /// The fully-qualified name of the function, used as the key in <see cref="FunctionTable"/>.
    /// </summary>
    public string? Name;

    /// <summary>
    /// The CLR return type of the function. Defaults to <see cref="void"/>.
    /// </summary>
    public Type ReturnType = typeof(void);

    /// <summary>
    /// The delegate wrapping the function body, set for anonymous functions and built-in
    /// PHP functions registered via delegates. Mutually exclusive with <see cref="MethodInfo"/>.
    /// </summary>
    public Delegate? Method;

    /// <summary>
    /// The <see cref="System.Reflection.MethodInfo"/> for the compiled function body, set for
    /// named functions compiled into a <see cref="System.Reflection.Emit.DynamicMethod"/>.
    /// Mutually exclusive with <see cref="Method"/>.
    /// </summary>
    public System.Reflection.MethodInfo? MethodInfo;

    /// <summary>
    /// The CLR parameter types of the function, in declaration order. Variadic parameters
    /// are represented as <see cref="object"/>[]. May be <see langword="null"/> for functions
    /// with no parameters.
    /// </summary>
    public Type[]? ParameterTypes;
}