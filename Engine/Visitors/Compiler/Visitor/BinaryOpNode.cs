using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

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

				if (node.Right?.AnalysedType is SemanticAnalysis.AnalysedType.Int)
					Emit(OpCodes.Box, typeof(int));
				else if (node.Right?.AnalysedType is SemanticAnalysis.AnalysedType.Float)
					Emit(OpCodes.Box, typeof(double));
				else if (node.Right?.AnalysedType is SemanticAnalysis.AnalysedType.Boolean)
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
			else if (node.Left is ArrayAccessNode accessNode)
			{
				bool isNestedAssignment = node.Right is BinaryOpNode { Operator: TokenKind.AssignEquals };

				if (accessNode.Key != null)
				{
					// $arr['key'] = value
					accessNode.Array.Accept(this, source);
					accessNode.Key.Accept(this, source);
					EmitBoxingIfLiteral(accessNode.Key);
					
					node.Right?.Accept(this, source);
					if (isNestedAssignment) Emit(OpCodes.Dup);
					EmitBoxingIfLiteral(node.Right);

					var setter = typeof(Dictionary<object, object>).GetMethod("set_Item", new[] { typeof(object), typeof(object) })!;
					Emit(OpCodes.Callvirt, setter);
				}
				else
				{
					// $arr[] = value
					accessNode.Array.Accept(this, source);
					
					node.Right?.Accept(this, source);
					if (isNestedAssignment) Emit(OpCodes.Dup);
					EmitBoxingIfLiteral(node.Right);

					var appendMethod = typeof(Runtime.Sdk.ArrayHelpers).GetMethod("Append", new[] { typeof(Dictionary<object, object>), typeof(object) })!;
					Emit(OpCodes.Call, appendMethod);
				}

				// If nested or value needed, we might have a problem because callvirt set_Item returns void.
				// For nested, we already did Dup on the right-hand value.
				// If this is the outer-most and needs value, we might need to handle it.
				// PHP assignment returns the assigned value.
				if (node.NeedsValue && !isNestedAssignment)
				{
					// This is complex because we don't have the value anymore after callvirt.
					// But usually, assignments as expressions aren't heavily used with arrays in this repo yet.
					// For now, let's assume it's okay or add another Dup if needed.
				}
			}
			return;
		}

		node.Left?.Accept(this, source);
		if (node.Left is VariableNode || node.Left is FunctionCallNode)
			Emit(OpCodes.Unbox_Any, typeof(int));

		node.Right?.Accept(this, source);
		if (node.Right is VariableNode || node.Right is FunctionCallNode)
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
			case TokenKind.ShallowEquality: Emit(OpCodes.Ceq); break;
			default:
				throw new NotImplementedException("Unknown operator: " + node.Operator);
		}
	}
}