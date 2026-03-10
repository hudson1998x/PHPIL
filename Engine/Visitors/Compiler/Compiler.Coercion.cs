using System.Reflection.Emit;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    private void EmitStringCoercion(AnalysedType type, bool isVariable = false)
    {
        switch (type)
        {
            case AnalysedType.String:
                return;

            case AnalysedType.Int:
                if (isVariable)
                {
                    // Already boxed Object in local slot — just call ToString directly
                    Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                }
                else
                {
                    // Raw int from literal — must box first
                    Emit(OpCodes.Box, typeof(int));
                    Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                }
                break;

            case AnalysedType.Float:
                if (isVariable)
                {
                    Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                }
                else
                {
                    Emit(OpCodes.Box, typeof(double));
                    Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                }
                break;

            case AnalysedType.Mixed:
                // Already a boxed Object reference
                Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                break;

            default:
                throw new NotImplementedException($"Cannot coerce {type} to string yet.");
        }
    }
}