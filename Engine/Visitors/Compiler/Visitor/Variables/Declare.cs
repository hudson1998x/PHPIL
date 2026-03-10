using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
        if (node.VariableValue is VariableDeclaration childDeclaration)
        {
            childDeclaration.Accept(this, source);

            node.Local = DeclareLocal(typeof(object));
            _locals[node.VariableName.TextValue(in source)] = node.Local;

            Emit(OpCodes.Ldloc, childDeclaration.Local!);
            Emit(OpCodes.Stloc, node.Local);

            if (node.EmitValue)
                Emit(OpCodes.Ldloc, node.Local);

            return;
        }

        if (node.VariableValue is not null)
            node.VariableValue.Accept(this, source);
        else
            Emit(OpCodes.Ldnull);

        node.Local = DeclareLocal(typeof(object));
        _locals[node.VariableName.TextValue(in source)] = node.Local;

        switch (node.AnalysedType)
        {
            case AnalysedType.Int:     Emit(OpCodes.Box, typeof(int));    break;
            case AnalysedType.Float:   Emit(OpCodes.Box, typeof(double)); break;
            case AnalysedType.Boolean: Emit(OpCodes.Box, typeof(bool));   break;
            case AnalysedType.Mixed:   Emit(OpCodes.Box, typeof(int));    break;
        }

        Emit(OpCodes.Stloc, node.Local);

        if (node.EmitValue)
            Emit(OpCodes.Ldloc, node.Local);
    }
}