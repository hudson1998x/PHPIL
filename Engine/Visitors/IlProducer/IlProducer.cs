using System.Reflection.Emit;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors.IlProducer;

/// <summary>
/// The main IL-emitting visitor. Walks the AST produced by the parser and emits
/// .NET IL instructions via <see cref="ILSpy"/> (which wraps an <see cref="ILGenerator"/>),
/// ultimately producing executable code from a PHP source file.
///
/// <para>
/// This is a <c>partial</c> class — the node-specific <c>Visit</c> overloads
/// (one per AST node type) live in separate files alongside their corresponding
/// node definitions, keeping emission logic co-located with the constructs it
/// handles rather than centralised in one enormous file.
/// </para>
///
/// <para>
/// The default constructor targets a freshly created <see cref="DynamicMethod"/>
/// named <c>phpil_main</c> — a top-level void entry point with no parameters,
/// suitable for executing a PHP script directly. The secondary constructor accepts
/// an external <see cref="ILGenerator"/> for cases where the caller controls the
/// method being built, such as emitting a named function body into a
/// <see cref="System.Reflection.Emit.TypeBuilder"/>.
/// </para>
/// </summary>
public partial class IlProducer : IVisitor
{
    /// <summary>
    /// Tracks declared locals, function definitions, and the current variable scope
    /// stack across the traversal. Shared across all Visit calls so that nested
    /// constructs (function bodies, loops, if branches) can resolve variables
    /// declared in outer scopes.
    /// </summary>
    private readonly RuntimeContext _context;

    /// <summary>
    /// The dynamic method being built when using the default constructor.
    /// <c>null</c> when an external <see cref="ILGenerator"/> was injected —
    /// in that case the caller owns the method and <see cref="Execute"/> should
    /// not be called.
    /// </summary>
    private DynamicMethod? _runtimeMethod;

    /// <summary>The underlying IL generator — either from the dynamic method or injected externally.</summary>
    private ILGenerator _ilGenerator;

    /// <summary>
    /// Logging wrapper around <see cref="_ilGenerator"/>. All emission goes through
    /// this so the full instruction stream can be inspected for debugging without
    /// changing any call sites.
    /// </summary>
    private ILSpy _ilSpy;

    /// <summary>
    /// Tracks the CLR type most recently pushed onto the evaluation stack by a
    /// Visit call. Used by parent nodes to determine what coercions or operations
    /// are valid on the value their child just emitted — for example, deciding
    /// whether to emit a numeric add or a string concatenation.
    /// </summary>
    public Type? LastEmittedType { get; set; }

    /// <summary>
    /// Default constructor — creates a self-contained entry point method
    /// (<c>phpil_main</c>) that can be invoked directly via <see cref="Execute"/>.
    /// Use this when running a top-level PHP script.
    /// </summary>
    public IlProducer()
    {
        _context = new RuntimeContext();
        _runtimeMethod = new DynamicMethod(
            "phpil_main",
            typeof(void),
            []           // no parameters — scripts run in a closed environment
        );
        _ilGenerator = _runtimeMethod.GetILGenerator();
        _ilSpy = new ILSpy(_ilGenerator, true);
    }

    /// <summary>
    /// Secondary constructor for emitting into an externally managed method body,
    /// such as a named function being compiled into a <see cref="System.Reflection.Emit.TypeBuilder"/>.
    /// The caller is responsible for emitting <c>ret</c> and finalising the method —
    /// <see cref="Execute"/> must not be called on instances created this way.
    /// </summary>
    public IlProducer(ILGenerator ilGenerator)
    {
        _context = new RuntimeContext();
        _ilGenerator = ilGenerator;
        _ilSpy = new ILSpy(_ilGenerator, true);
    }

    /// <summary>
    /// Pushes a new variable scope frame onto the runtime context stack.
    /// Called when entering any construct that introduces a new lexical scope —
    /// function bodies, potentially loops or blocks — so that variables declared
    /// inside don't leak into the enclosing scope.
    /// </summary>
    public void EnterScope()
    {
        _context.PushFrame(new StackFrame());
    }

    /// <summary>
    /// Exposes the <see cref="RuntimeContext"/> to sub-producers (e.g. function
    /// declaration visitors) that need to share the same scope and variable state
    /// rather than starting from a blank context.
    /// </summary>
    public RuntimeContext GetContext()
    {
        return _context;
    }

    /// <summary>
    /// Exposes the <see cref="ILSpy"/> wrapper so that sub-producers compiling
    /// nested constructs (e.g. a function body emitted by a separate
    /// <see cref="IlProducer"/> instance) can write into the same instruction
    /// stream rather than a different method.
    /// </summary>
    public ILSpy GetILGenerator()
    {
        return _ilSpy;
    }

    /// <summary>
    /// Base <see cref="IVisitor.Visit"/> implementation — forwards to
    /// <see cref="SyntaxNode.Accept"/> to trigger double dispatch, routing the
    /// node to the most specific typed overload available in the partial class.
    /// </summary>
    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        node.Accept(this, in span);
    }

    /// <summary>
    /// Pops the current scope frame when leaving a lexical scope, discarding any
    /// locals declared within it. Must be paired with every <see cref="EnterScope"/>
    /// call — unbalanced scope push/pop would corrupt the variable resolution stack.
    /// </summary>
    public void ExitScope()
    {
        _context.PopFrame();
    }

    /// <summary>
    /// Finalises the dynamic method by emitting a <c>ret</c> instruction and then
    /// invokes it immediately. Only valid when using the default constructor — if an
    /// external <see cref="ILGenerator"/> was injected, <c>_runtimeMethod</c> is
    /// <c>null</c> and the invoke is a no-op.
    ///
    /// <para>
    /// The <c>ret</c> instruction is emitted here rather than at the end of the
    /// AST traversal because the visitor has no reliable way to know when the last
    /// statement has been visited — <see cref="Execute"/> is the explicit signal
    /// that emission is complete.
    /// </para>
    /// </summary>
    public void Execute()
    {
        _ilGenerator.Emit(OpCodes.Ret);
        _runtimeMethod?.Invoke(null, null);
    }
    
    // The stack of active loops. The top of the stack (Peek) is always 
    // the innermost loop relative to the current visitor position.
    private readonly Stack<LoopContext> _loopStack = new();

    /// <summary>
    /// Pushes a new loop's jump targets onto the stack. 
    /// Should be called by loop nodes (While, For, Foreach) before visiting their body.
    /// </summary>
    public void PushLoop(LoopContext context) => _loopStack.Push(context);

    /// <summary>
    /// Removes the current loop targets from the stack.
    /// Should be called by loop nodes after the body has been visited.
    /// </summary>
    public void PopLoop() => _loopStack.Pop();

    /// <summary>
    /// Resolves a loop context based on the PHP nesting level (e.g., 'break 2;').
    /// PHP levels are 1-based, where 1 is the innermost loop.
    /// </summary>
    /// <param name="level">The number of nesting levels to look back.</param>
    /// <returns>The targeted LoopContext, or null if the level exceeds the stack depth.</returns>
    public LoopContext? GetLoopAtLevel(int level)
    {
        if (level <= 0 || level > _loopStack.Count) 
            return null;

        // Converting to array allows us to index from the top (Innermost = Index 0)
        // without destructive popping.
        var frames = _loopStack.ToArray();
        return frames[level - 1];
    }
}