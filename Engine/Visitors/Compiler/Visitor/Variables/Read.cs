using System.Reflection.Emit;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    private static readonly HashSet<string> Superglobals = new(StringComparer.OrdinalIgnoreCase)
    {
        "$_GET", "$_POST", "$_COOKIE", "$_SERVER", "$_REQUEST", "$_FILES", "$_ENV", "$_SESSION"
    };

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