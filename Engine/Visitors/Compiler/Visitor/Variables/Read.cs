using System.Reflection.Emit;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// The set of PHP superglobal variable names, matched case-insensitively.
    /// </summary>
    private static readonly HashSet<string> Superglobals = new(StringComparer.OrdinalIgnoreCase)
    {
        "$_GET", "$_POST", "$_COOKIE", "$_SERVER", "$_REQUEST", "$_FILES", "$_ENV", "$_SESSION"
    };

    /// <summary>
    /// Emits IL to load the value of a variable onto the stack.
    /// </summary>
    /// <param name="node">The <see cref="VariableNode"/> representing the variable reference.</param>
    /// <param name="source">The original source text, used to resolve the variable name.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>GlobalState.GetSuperglobal</c> cannot be located via reflection.
    /// </exception>
    /// <exception cref="Exception">Thrown when the variable has not been declared in the current scope.</exception>
    /// <remarks>
    /// Variables are resolved in the following order:
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <b>Superglobals</b> (<c>$_GET</c>, <c>$_POST</c>, etc.) — loaded via
    ///       <c>GlobalState.GetSuperglobal</c>, which retrieves the value from the runtime's
    ///       global state dictionary.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b><c>$this</c></b> — within an instance method, loaded from the <c>$this</c>
    ///       local if one was declared by <see cref="VisitClassNode"/>, otherwise loaded
    ///       directly from <see cref="OpCodes.Ldarg_0"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Local variables</b> — loaded from the <c>LocalBuilder</c> slot registered
    ///       in <c>_locals</c> during declaration.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source)
    {
        var varName = node.Token.TextValue(in source);

        // Handle superglobals - access via GlobalState
        if (Superglobals.Contains(varName))
        {
            // Emit: GlobalState.GetSuperglobal("$_GET")
            Emit(OpCodes.Ldstr, varName);
            var getSuperglobalMethod = typeof(GlobalState).GetMethod("GetSuperglobal", [typeof(string)]);
            if (getSuperglobalMethod == null)
                throw new InvalidOperationException("GlobalState.GetSuperglobal method not found");
            Emit(OpCodes.Call, getSuperglobalMethod);
            return;
        }

        // Handle $this - check locals first (new approach), fallback to ldarg_0 (original)
        if (varName == "$this" && !_isStaticMethod && _currentType != null)
        {
            if (_locals.TryGetValue("$this", out var thisLocal))
                Emit(OpCodes.Ldloc, thisLocal);
            else
                Emit(OpCodes.Ldarg_0);
            return;
        }

        if (!_locals.TryGetValue(varName, out var local))
            throw new Exception($"Undefined variable: {varName}");

        Emit(OpCodes.Ldloc, local);
    }
}