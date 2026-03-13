using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source)
    {
        var varName = node.Token.TextValue(in source);

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