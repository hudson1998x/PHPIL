using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source)
    {
        if (!_locals.TryGetValue(node.Token.TextValue(in source), out var local))
            throw new Exception($"Undefined variable: {node.Token.TextValue(in source)}");

        Emit(OpCodes.Ldloc, local);
    }
}