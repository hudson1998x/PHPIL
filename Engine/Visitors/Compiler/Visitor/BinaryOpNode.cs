using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    private static readonly MethodInfo StringConcat =
        typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })!;
    
	public void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source)
	{
		if (node.Operator is TokenKind.Concat)
		{
			node.Left?.Accept(this, source);
			EmitStringCoercion(node.Left!.AnalysedType, isVariable: node.Left is VariableNode);

			if (node.Right is BinaryOpNode { Operator: TokenKind.AssignEquals } innerAssign)
			{
				innerAssign.NeedsValue = true;
			}

			node.Right?.Accept(this, source);

			EmitStringCoercion(node.Right!.AnalysedType, isVariable: node.Right is VariableNode);

			Emit(OpCodes.Call, StringConcat);
			return;
		}

		if (node.Operator is TokenKind.AssignEquals)
		{
			if (node.Left is VariableNode varNode)
			{
				var varName = varNode.Token.TextValue(in source);
				bool isNestedAssignment = node.Right is BinaryOpNode { Operator: TokenKind.AssignEquals };

				node.Right?.Accept(this, source);

				if (isNestedAssignment)
					Emit(OpCodes.Dup);

				if (node.Right?.AnalysedType is AnalysedType.Int)
					Emit(OpCodes.Box, typeof(int));
				else if (node.Right?.AnalysedType is AnalysedType.Float)
					Emit(OpCodes.Box, typeof(double));
				else if (node.Right?.AnalysedType is AnalysedType.Boolean)
					Emit(OpCodes.Box, typeof(bool));

				if (!_locals.TryGetValue(varName, out var local))
				{
					local = DeclareLocal(typeof(object));
					_locals[varName] = local;
				}

				Emit(OpCodes.Stloc, local);

				if (isNestedAssignment || node.NeedsValue)
					Emit(OpCodes.Ldloc, local);
			}
			return;
		}

		node.Left?.Accept(this, source);
		if (node.Left is VariableNode)
			Emit(OpCodes.Unbox_Any, typeof(int));

		node.Right?.Accept(this, source);
		if (node.Right is VariableNode)
			Emit(OpCodes.Unbox_Any, typeof(int));

		switch (node.Operator)
		{
			case TokenKind.Multiply:    Emit(OpCodes.Mul); break;
			case TokenKind.Add:         Emit(OpCodes.Add); break;
			case TokenKind.Subtract:    Emit(OpCodes.Sub); break;
			case TokenKind.DivideBy:    Emit(OpCodes.Div); break;
			case TokenKind.Modulo:      Emit(OpCodes.Rem); break;
			case TokenKind.LessThan:    Emit(OpCodes.Clt); break;
			case TokenKind.GreaterThan: Emit(OpCodes.Cgt); break;
			default:
				throw new NotImplementedException("Unknown operator: " + node.Operator);
		}
	}
}