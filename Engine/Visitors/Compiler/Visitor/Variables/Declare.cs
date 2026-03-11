using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
	public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
	{
		var varName = node.VariableName.TextValue(in source);

		if (node.VariableValue is VariableDeclaration childDeclaration)
		{
			childDeclaration.Accept(this, source);

			if (!_locals.TryGetValue(varName, out var existingLocal))
			{
				node.Local = DeclareLocal(typeof(object));
				_locals[varName] = node.Local;
			}
			else
			{
				node.Local = existingLocal;
			}

			if (node.EmitValue)
				Emit(OpCodes.Dup);

			Emit(OpCodes.Stloc, node.Local);

			return;
		}

		if (node.VariableValue is not null)
			node.VariableValue.Accept(this, source);
		else
			Emit(OpCodes.Ldnull);

		if (!_locals.TryGetValue(varName, out var existingLocal2))
		{
			node.Local = DeclareLocal(typeof(object));
			_locals[varName] = node.Local;
		}
		else
		{
			node.Local = existingLocal2;
		}

		switch (node.AnalysedType)
		{
			case AnalysedType.Int:     Emit(OpCodes.Box, typeof(int));    break;
			case AnalysedType.Float:   Emit(OpCodes.Box, typeof(double)); break;
			case AnalysedType.Boolean: Emit(OpCodes.Box, typeof(bool));   break;
			case AnalysedType.Mixed:   Emit(OpCodes.Box, typeof(int));    break;
		}

		if (node.EmitValue)
			Emit(OpCodes.Dup);

		Emit(OpCodes.Stloc, node.Local);
	}
}