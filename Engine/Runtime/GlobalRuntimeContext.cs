using PHPIL.Engine.Runtime.Types;

namespace PHPIL.Engine.Runtime;

/// <summary>
/// Static global state shared across the entire PHPIL runtime. Holds the function
/// table, compile-time constants, and the standard output stream used by all
/// executing scripts.
///
/// <para>
/// All members are intentionally static — there is exactly one runtime context
/// per process, matching PHP's single-threaded execution model. Concurrent script
/// execution is not supported.
/// </para>
/// </summary>
public static partial class GlobalRuntimeContext
{
    /// <summary>
    /// The global function table — every named PHP function declared or registered
    /// during execution lives here, keyed by its unqualified name.
    ///
    /// <para>
    /// Populated at declaration time by <see cref="PHPIL.Engine.SyntaxTree.FunctionNode"/>
    /// and looked up at call time by <see cref="PHPIL.Engine.SyntaxTree.FunctionCallNode"/>.
    /// Last-write wins — re-declaring a function replaces the previous definition,
    /// matching PHP's own runtime behaviour.
    /// </para>
    /// </summary>
    public static readonly Dictionary<string, PhpFunction> FunctionTable = [];

    /// <summary>
    /// Compile-time constant pool — a flat list of <see cref="PhpValue"/> instances
    /// (typically compiled closures) that cannot be embedded directly as IL operands.
    ///
    /// <para>
    /// Each entry is registered at compile time and retrieved at runtime via an
    /// index operand emitted into the IL stream. This bridges the gap between
    /// <see cref="System.Reflection.Emit.DynamicMethod"/> IL, which cannot embed
    /// arbitrary managed object references, and the closures that need to be
    /// available when the emitted code executes.
    /// </para>
    /// </summary>
    public static readonly List<PhpValue> Constants = new();

    /// <summary>
    /// The underlying memory buffer backing <see cref="Stdout"/>. Can be read
    /// after execution to capture all output produced by the script — useful for
    /// testing and non-interactive environments where writing directly to the
    /// console is undesirable.
    /// </summary>
    public static readonly MemoryStream StdoutStream = new();

    /// <summary>
    /// The standard output writer used by all PHP output constructs (<c>echo</c>,
    /// <c>print</c>, etc.). Wraps <see cref="StdoutStream"/> with auto-flush enabled
    /// so output is visible immediately without explicit flushing.
    /// </summary>
    public static readonly StreamWriter Stdout = new(StdoutStream) { AutoFlush = true };

    /// <summary>
    /// Initialises the runtime by registering all built-in standard library
    /// functions into <see cref="FunctionTable"/> before any user script executes.
    /// </summary>
    static GlobalRuntimeContext()
    {
        Stdlib_Dev();
        Stdlib_ReqInc();
        Stdlib_Strings();
    }
}