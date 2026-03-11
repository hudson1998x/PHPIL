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

	private void EmitArrayToString()
	{
		Emit(OpCodes.Ldstr, "Array");
	}
}
