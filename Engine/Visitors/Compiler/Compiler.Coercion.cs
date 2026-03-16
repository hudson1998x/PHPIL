using System.Reflection.Emit;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL to coerce the value on top of the stack to a <see cref="string"/>,
    /// matching PHP's implicit string casting rules.
    /// </summary>
    /// <param name="type">The <see cref="AnalysedType"/> of the value currently on the stack.</param>
    /// <param name="isVariable">
    /// When <see langword="true"/>, the value is already boxed as <see cref="object"/> and
    /// <c>ToString</c> can be called directly. When <see langword="false"/>, unboxed value
    /// types (<see cref="int"/>, <see cref="double"/>) are boxed first.
    /// </param>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description><see cref="AnalysedType.String"/> — no-op; the value is already a string.</description></item>
    ///   <item><description><see cref="AnalysedType.Int"/> — boxed to <see cref="int"/> if not a variable, then <c>ToString</c> called via <see cref="OpCodes.Callvirt"/>.</description></item>
    ///   <item><description><see cref="AnalysedType.Float"/> — boxed to <see cref="double"/> if not a variable, then <c>ToString</c> called via <see cref="OpCodes.Callvirt"/>.</description></item>
    ///   <item><description><see cref="AnalysedType.Mixed"/> and all other types — <c>ToString</c> called directly, assuming the value is already a reference type.</description></item>
    ///   <item><description><see cref="AnalysedType.Array"/> — delegates to <see cref="EmitArrayToString"/>.</description></item>
    /// </list>
    /// </remarks>
    private void EmitStringCoercion(AnalysedType type, bool isVariable = false)
    {
        switch (type)
        {
            case AnalysedType.String:
                return;

            case AnalysedType.Int:
                if (isVariable)
                {
                    Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                }
                else
                {
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
                Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                break;

            case AnalysedType.Array:
                EmitArrayToString();
                break;

            default:
                Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
                break;
        }
    }

    /// <summary>
    /// Emits IL to replace an array value on the stack with the string <c>"Array"</c>,
    /// matching PHP's behaviour when an array is coerced to a string.
    /// </summary>
    private void EmitArrayToString()
    {
        Emit(OpCodes.Ldstr, "Array");
    }
}