using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

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

        if (node.Operator is TokenKind.LogicalAnd or TokenKind.LogicalAndKeyword or TokenKind.LogicalOr or TokenKind.LogicalOrKeyword)
        {
            var endLabel = DefineLabel();
            node.Left?.Accept(this, source);
            EmitBoxingIfLiteral(node.Left);

            Emit(OpCodes.Dup);
            EmitCoerceToBool();
            
            if (node.Operator is TokenKind.LogicalAnd or TokenKind.LogicalAndKeyword)
                Emit(OpCodes.Brfalse, endLabel);
            else
                Emit(OpCodes.Brtrue, endLabel);
            
            Emit(OpCodes.Pop);
            
            if (node.Right is BinaryOpNode rightBin)
                rightBin.NeedsValue = true;

            node.Right?.Accept(this, source);
            EmitBoxingIfLiteral(node.Right);
            
            MarkLabel(endLabel);
            return;
        }

		if (node.Operator is TokenKind.AssignEquals)
		{
            HandleAssignment(node, source);
            return;
		}

        if (IsCompoundAssignment(node.Operator))
        {
            // Compound assignment: $x += 5
            var baseOp = GetBaseOperator(node.Operator);
            
            // 1. Evaluate LHS (loads current value)
            node.Left?.Accept(this, source);
            if (node.Left is VariableNode or FunctionCallNode)
                Emit(OpCodes.Unbox_Any, GetTypeForOp(baseOp));

            // 2. Evaluate RHS
            node.Right?.Accept(this, source);
            if (node.Right is VariableNode or FunctionCallNode)
                Emit(OpCodes.Unbox_Any, GetTypeForOp(baseOp));

            // 3. Perform operation
            EmitOperator(baseOp);
            
            // 4. Box result
            Emit(OpCodes.Box, GetTypeForOp(baseOp));
            
            // 5. Store result back to LHS
            StoreToLHS(node.Left!, source, node.NeedsValue);
            return;
        }

        if (node.Operator is TokenKind.DeepEquality or TokenKind.DeepInequality)
        {
            node.Left?.Accept(this, source);
            EmitBoxingIfLiteral(node.Left);
            node.Right?.Accept(this, source);
            EmitBoxingIfLiteral(node.Right);
            
            var strictMethod = typeof(Runtime.Runtime).GetMethod("StrictEquals", BindingFlags.Public | BindingFlags.Static)!;
            Emit(OpCodes.Call, strictMethod);
            
            if (node.Operator == TokenKind.DeepInequality)
            {
                Emit(OpCodes.Ldc_I4_0);
                Emit(OpCodes.Ceq);
            }
            Emit(OpCodes.Box, typeof(bool));
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
            case TokenKind.LogicalXorKeyword: Emit(OpCodes.Xor); break;
			default:
				throw new NotImplementedException("Unknown operator: " + node.Operator);
		}
	}

    private void HandleAssignment(BinaryOpNode node, in ReadOnlySpan<char> source)
    {
        if (node.Left is VariableNode varNode)
        {
            var varName = varNode.Token.TextValue(in source);
            bool isNestedAssignment = node.Right is BinaryOpNode { Operator: var op } && IsAssignment(op);

            node.Right?.Accept(this, source);

            if (isNestedAssignment)
                Emit(OpCodes.Dup);

            EmitBoxingIfLiteral(node.Right);

            if (!_locals.TryGetValue(varName, out var local))
            {
                local = DeclareLocal(typeof(object));
                _locals[varName] = local;
            }

            Emit(OpCodes.Stloc, local);

            if (node.NeedsValue)
                Emit(OpCodes.Ldloc, local);
        }
        else if (node.Left is ArrayAccessNode accessNode)
        {
            bool isNestedAssignment = node.Right is BinaryOpNode { Operator: var op } && IsAssignment(op);

            if (accessNode.Key != null)
            {
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
                accessNode.Array.Accept(this, source);
                
                node.Right?.Accept(this, source);
                if (isNestedAssignment) Emit(OpCodes.Dup);
                EmitBoxingIfLiteral(node.Right);

                var appendMethod = typeof(Runtime.Sdk.ArrayHelpers).GetMethod("Append", new[] { typeof(Dictionary<object, object>), typeof(object) })!;
                Emit(OpCodes.Call, appendMethod);
            }
        }
        else if (node.Left is ObjectAccessNode objAccess)
        {
            // $obj->prop = value
            objAccess.Object?.Accept(this, source);

            var propName = objAccess.Property.Token.TextValue(in source);
            
            // Push property name BEFORE value so stack order is correct for SetProperty(obj, propName, value)
            // Stack order needed: [obj, propName, value]
            Emit(OpCodes.Ldstr, propName);
            
            node.Right?.Accept(this, source);
            EmitBoxingIfLiteral(node.Right);

            if (node.NeedsValue) Emit(OpCodes.Dup);

            // Check if it's $this and we have the field builder
            if (objAccess.Object is VariableNode vn && vn.Token.TextValue(in source) == "$this" && _currentType != null)
            {
                var fqn = _currentType.Name.Replace(".", "\\");
                var phpType = TypeTable.GetType(fqn);
                if (phpType != null && phpType.FieldBuilders.TryGetValue(propName, out var fieldBuilder))
                {
                    // Direct field assignment
                    Emit(OpCodes.Stfld, fieldBuilder);
                    return;
                }
            }
            
            // Fall back to runtime helper for dynamic property write
            var setPropMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("SetProperty", new[] { typeof(object), typeof(string), typeof(object) })!;
            Emit(OpCodes.Call, setPropMethod);
        }
        else if (node.Left is StaticAccessNode staticAccess)
        {
            // Class::$prop = value
            node.Right?.Accept(this, source);
            EmitBoxingIfLiteral(node.Right);
            if (node.NeedsValue) Emit(OpCodes.Dup);

            string? memberName = null;
            if (staticAccess.MemberName is IdentifierNode idNode)
                memberName = idNode.Token.TextValue(in source);
            else if (staticAccess.MemberName is VariableNode staticVarNode)
                memberName = staticVarNode.Token.TextValue(in source);
                
            if (memberName == null)
                throw new Exception("Cannot resolve static property name");
                
            Type? targetType = null;
            if (staticAccess.Target is QualifiedNameNode qname)
            {
                var fqn = ResolveFQN(qname, source);
                targetType = TypeTable.GetType(fqn)?.RuntimeType;
            }

            if (targetType != null)
            {
                var field = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (field != null)
                {
                    Emit(OpCodes.Stsfld, (System.Reflection.FieldInfo)field);
                    return;
                }
            }
            throw new NotImplementedException($"Static property write '{memberName}' not found.");
        }
    }

    private void StoreToLHS(SyntaxNode lhs, in ReadOnlySpan<char> source, bool needsValue)
    {
        if (lhs is VariableNode varNode)
        {
            var varName = varNode.Token.TextValue(in source);
            if (!_locals.TryGetValue(varName, out var local))
            {
                local = DeclareLocal(typeof(object));
                _locals[varName] = local;
            }
            if (needsValue) Emit(OpCodes.Dup);
            Emit(OpCodes.Stloc, local);
        }
        else if (lhs is ArrayAccessNode accessNode)
        {
            // For compound assign to array access, it's a bit more complex.
            // We need to re-evaluate the array and key.
            // But for now, let's keep it simple or implement it if needed.
            throw new NotImplementedException("Compound assignment to array accessor not implemented yet.");
        }
    }

    private bool IsAssignment(TokenKind kind) => kind is
        TokenKind.AssignEquals or TokenKind.AddAssign or TokenKind.SubtractAssign or
        TokenKind.MultiplyAssign or TokenKind.DivideAssign or TokenKind.ModuloAssign or
        TokenKind.PowerAssign or TokenKind.ConcatAppend or TokenKind.NullCoalesceAssign;

    private bool IsCompoundAssignment(TokenKind kind) => kind is
        TokenKind.AddAssign or TokenKind.SubtractAssign or TokenKind.MultiplyAssign or 
        TokenKind.DivideAssign or TokenKind.ModuloAssign or TokenKind.PowerAssign or 
        TokenKind.ConcatAppend or TokenKind.NullCoalesceAssign;

    private TokenKind GetBaseOperator(TokenKind compoundOp) => compoundOp switch
    {
        TokenKind.AddAssign => TokenKind.Add,
        TokenKind.SubtractAssign => TokenKind.Subtract,
        TokenKind.MultiplyAssign => TokenKind.Multiply,
        TokenKind.DivideAssign => TokenKind.DivideBy,
        TokenKind.ModuloAssign => TokenKind.Modulo,
        TokenKind.PowerAssign => TokenKind.Power,
        TokenKind.ConcatAppend => TokenKind.Concat,
        _ => throw new ArgumentException()
    };

    private Type GetTypeForOp(TokenKind op) => op switch
    {
        TokenKind.DivideBy => typeof(double),
        TokenKind.Concat => typeof(string),
        _ => typeof(int) // Simplified
    };

    private void EmitOperator(TokenKind op)
    {
        switch (op)
        {
            case TokenKind.Add: Emit(OpCodes.Add); break;
            case TokenKind.Subtract: Emit(OpCodes.Sub); break;
            case TokenKind.Multiply: Emit(OpCodes.Mul); break;
            case TokenKind.DivideBy: Emit(OpCodes.Div); break;
            case TokenKind.Modulo: Emit(OpCodes.Rem); break;
            case TokenKind.Power: // Need a helper for power
                Emit(OpCodes.Call, typeof(Math).GetMethod("Pow", new[] { typeof(double), typeof(double) })!);
                break;
            case TokenKind.Concat:
                Emit(OpCodes.Call, StringConcat);
                break;
            default: throw new NotImplementedException();
        }
    }
}
