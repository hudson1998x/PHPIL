using System.Reflection.Emit;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    private void EmitStringCoercion(AnalysedType type)
    {
        switch (type)
        {
            case AnalysedType.String:
                return; // already a string

            case AnalysedType.Int:
                Emit(OpCodes.Box, typeof(int));
                Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                break;

            case AnalysedType.Float:
                Emit(OpCodes.Box, typeof(double));
                Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                break;
            case AnalysedType.Mixed:
                Emit(OpCodes.Box, typeof(object));
                Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                break;

            default:
                throw new NotImplementedException($"Cannot coerce {type} to string yet.");
        }
    }
}