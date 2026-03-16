using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Cached <see cref="MethodInfo"/> for <c>string.Concat(string, string)</c>, used by
    /// string concatenation and compound concat-assign emission.
    /// </summary>
    private static readonly MethodInfo StringConcat =
        typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })!;

    /// <summary>
    /// Emits IL for a binary operator expression, dispatching to specialised handlers based
    /// on the operator kind.
    /// </summary>
    /// <param name="node">The <see cref="BinaryOpNode"/> representing the binary expression.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    /// <exception cref="NotImplementedException">Thrown for unrecognised operator kinds.</exception>
    /// <remarks>
    /// Operators are handled in the following priority order:
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <b>Concatenation (<c>.</c>)</b> — both operands are coerced to <see cref="string"/>
    ///       via <c>EmitStringCoercion</c> and combined with <see cref="StringConcat"/>. If the
    ///       right operand is an assignment expression, <c>NeedsValue</c> is set so its result
    ///       remains on the stack.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Logical <c>&amp;&amp;</c> / <c>and</c> / <c>||</c> / <c>or</c></b> — short-circuit
    ///       evaluation: the left operand is duplicated and coerced to <see cref="bool"/>; a
    ///       <see cref="OpCodes.Brfalse"/> (for <c>&amp;&amp;</c>) or <see cref="OpCodes.Brtrue"/>
    ///       (for <c>||</c>) skips the right operand if the result is already determined. The
    ///       surviving value is left on the stack as the expression result.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Assignment (<c>=</c>)</b> — delegated to <see cref="HandleAssignment"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Null coalesce (<c>??</c>)</b> — both operands are evaluated and boxed, then
    ///       passed to <c>Runtime.NullCoalesce</c>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Null coalesce assign (<c>??=</c>)</b> — the left variable's current value is
    ///       tested; if non-null it is kept (and optionally duplicated for expression use);
    ///       otherwise the right operand is evaluated, stored, and left on the stack.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Compound assignment (<c>+=</c>, <c>-=</c>, etc.)</b> — the base operator is
    ///       derived via <see cref="GetBaseOperator"/>, the LHS and RHS are unboxed to the
    ///       appropriate type, the operation is emitted, the result is reboxed, and
    ///       <see cref="StoreToLHS"/> writes it back.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Strict equality / inequality (<c>===</c> / <c>!==</c>)</b> — both operands are
    ///       boxed and passed to <c>Runtime.StrictEquals</c>; inequality inverts the result via
    ///       <see cref="OpCodes.Ceq"/> against zero.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Arithmetic and comparison operators</b> — both operands are unboxed to
    ///       <see cref="int"/> (variables and function calls only) and the corresponding IL
    ///       opcode is emitted. <c>&lt;=</c> and <c>&gt;=</c> are synthesised by inverting
    ///       <see cref="OpCodes.Cgt"/> and <see cref="OpCodes.Clt"/> respectively.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
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

        if (node.Operator is TokenKind.NullCoalesce)
        {
            node.Left?.Accept(this, source);
            EmitBoxingIfLiteral(node.Left);
            node.Right?.Accept(this, source);
            EmitBoxingIfLiteral(node.Right);

            var nullCoalesce = typeof(Runtime.Runtime).GetMethod("NullCoalesce", BindingFlags.Public | BindingFlags.Static)!;
            Emit(OpCodes.Call, nullCoalesce);
            return;
        }

        if (node.Operator is TokenKind.NullCoalesceAssign)
        {
            if (node.Left is not VariableNode varNode)
                throw new NotImplementedException("Null coalesce assignment only supports variables");

            var varName = varNode.Token.TextValue(in source);
            var endLabel = DefineLabel();
            var evaluateRightLabel = DefineLabel();

            if (!_locals.TryGetValue(varName, out var local))
            {
                local = DeclareLocal(typeof(object));
                _locals[varName] = local;
            }

            Emit(OpCodes.Ldloc, local);
            Emit(OpCodes.Dup);
            Emit(OpCodes.Brfalse, evaluateRightLabel);

            if (node.NeedsValue)
                Emit(OpCodes.Dup);
            Emit(OpCodes.Stloc, local);
            Emit(OpCodes.Br, endLabel);

            MarkLabel(evaluateRightLabel);
            Emit(OpCodes.Pop);
            node.Right?.Accept(this, source);
            EmitBoxingIfLiteral(node.Right);
            if (node.NeedsValue)
                Emit(OpCodes.Dup);
            Emit(OpCodes.Stloc, local);

            MarkLabel(endLabel);
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
            case TokenKind.LessThanOrEqual:
            {
                Emit(OpCodes.Cgt);
                Emit(OpCodes.Ldc_I4_0);
                Emit(OpCodes.Ceq);
                break;
            }
            case TokenKind.GreaterThanOrEqual:
            {
                Emit(OpCodes.Clt);
                Emit(OpCodes.Ldc_I4_0);
                Emit(OpCodes.Ceq);
                break;
            }
            case TokenKind.ShallowEquality:   Emit(OpCodes.Ceq); break;
            case TokenKind.LogicalXorKeyword: Emit(OpCodes.Xor); break;
            default:
                throw new NotImplementedException("Unknown operator: " + node.Operator);
        }
    }

    /// <summary>
    /// Emits IL for a simple or compound assignment expression, handling variable, array element,
    /// instance property, and static property targets.
    /// </summary>
    /// <param name="node">The <see cref="BinaryOpNode"/> whose operator is <c>AssignEquals</c>.</param>
    /// <param name="source">The original source text, used to resolve target names.</param>
    /// <exception cref="Exception">
    /// Thrown when the target is a superglobal (direct assignment is not permitted), when a static
    /// property name cannot be resolved, or when <c>self</c> is used outside a class context.
    /// </exception>
    /// <exception cref="NotImplementedException">
    /// Thrown when a static property write target cannot be resolved at compile time.
    /// </exception>
    /// <remarks>
    /// Assignment targets are handled as follows:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b>Variable (<c>$x = ...</c>)</b> — the right-hand side is evaluated; for chained
    ///       assignments the value is duplicated before boxing. The local is allocated or reused
    ///       and the value is stored. If <c>node.NeedsValue</c> is <see langword="true"/>, the
    ///       stored value is reloaded for use as an expression result.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Array element with key (<c>$arr[$k] = ...</c>)</b> — the array, key, and value
    ///       are pushed and <c>Dictionary.set_Item</c> is called via <see cref="OpCodes.Callvirt"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Array append (<c>$arr[] = ...</c>)</b> — the array and value are pushed and
    ///       <c>ArrayHelpers.Append</c> is called for PHP-style auto-indexing.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Instance property (<c>$obj->prop = ...</c>)</b> — the object, property name
    ///       string, and value are pushed. For <c>$this</c> with a known <c>FieldBuilder</c>,
    ///       a direct <see cref="OpCodes.Stfld"/> is emitted; otherwise
    ///       <c>RuntimeHelpers.SetProperty</c> is used.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Static property (<c>ClassName::$prop = ...</c>)</b> — the value is evaluated,
    ///       then stored via <see cref="OpCodes.Stsfld"/> if the field is resolved at compile
    ///       time, or via <c>RuntimeHelpers.SetStaticFieldByName</c> for types not yet fully
    ///       created.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    private void HandleAssignment(BinaryOpNode node, in ReadOnlySpan<char> source)
    {
        if (node.Left is VariableNode varNode)
        {
            var varName = varNode.Token.TextValue(in source);
            
            // Check if this is a superglobal
            if (Superglobals.Contains(varName))
            {
                // Cannot directly assign to a superglobal variable itself, only to array elements
                throw new Exception($"Cannot assign directly to superglobal '{varName}'. Use array syntax like {varName}['key'] = value");
            }
            
            bool isNestedAssignment = node.Right is BinaryOpNode { Operator: var op } && IsAssignment(op);

            node.Right?.Accept(this, source);

            if (isNestedAssignment)
                Emit(OpCodes.Dup);

            EmitBoxing(node.Right);

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
                // Special handling for "self" - resolve to current class
                if (fqn.Equals("self", StringComparison.OrdinalIgnoreCase))
                {
                    if (_currentType == null)
                        throw new Exception("'self' used outside of class context.");
                    targetType = _currentType;
                }
                else
                {
                    targetType = TypeTable.GetType(fqn)?.RuntimeType;
                }
            }

            if (targetType != null)
            {
                try
                {
                    var field = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        Emit(OpCodes.Stsfld, (System.Reflection.FieldInfo)field);
                        return;
                    }
                }
                catch (NotSupportedException)
                {
                    // Type not fully created yet - use runtime helper
                }
                
                // Use runtime helper for types not fully created
                var setStaticFieldMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("SetStaticFieldByName", new[] { typeof(string), typeof(string), typeof(object) })!;
                Emit(OpCodes.Ldstr, targetType.Name);
                Emit(OpCodes.Ldstr, memberName);
                Emit(OpCodes.Call, setStaticFieldMethod);
                return;
            }
            throw new NotImplementedException($"Static property write '{memberName}' not found.");
        }
    }

    /// <summary>
    /// Stores a value from the top of the stack into the specified left-hand side target.
    /// </summary>
    /// <param name="lhs">The <see cref="SyntaxNode"/> representing the assignment target.</param>
    /// <param name="source">The original source text, used to resolve the variable name.</param>
    /// <param name="needsValue">
    /// When <see langword="true"/>, the value is duplicated before storing so it remains on
    /// the stack as the expression result.
    /// </param>
    /// <exception cref="NotImplementedException">
    /// Thrown when the target is an <see cref="ArrayAccessNode"/>, which is not yet supported
    /// for compound assignment.
    /// </exception>
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

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="kind"/> is any assignment operator.
    /// </summary>
    private bool IsAssignment(TokenKind kind) => kind is
        TokenKind.AssignEquals or TokenKind.AddAssign or TokenKind.SubtractAssign or
        TokenKind.MultiplyAssign or TokenKind.DivideAssign or TokenKind.ModuloAssign or
        TokenKind.PowerAssign or TokenKind.ConcatAppend or TokenKind.NullCoalesceAssign;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="kind"/> is a compound assignment
    /// operator (e.g. <c>+=</c>, <c>-=</c>, <c>.=</c>).
    /// </summary>
    private bool IsCompoundAssignment(TokenKind kind) => kind is
        TokenKind.AddAssign or TokenKind.SubtractAssign or TokenKind.MultiplyAssign or 
        TokenKind.DivideAssign or TokenKind.ModuloAssign or TokenKind.PowerAssign or 
        TokenKind.ConcatAppend or TokenKind.NullCoalesceAssign;

    /// <summary>
    /// Returns the simple binary operator corresponding to the given compound assignment operator.
    /// </summary>
    /// <param name="compoundOp">A compound assignment <see cref="TokenKind"/>.</param>
    /// <returns>The base <see cref="TokenKind"/> (e.g. <c>AddAssign</c> → <c>Add</c>).</returns>
    /// <exception cref="ArgumentException">Thrown for unrecognised compound operators.</exception>
    private TokenKind GetBaseOperator(TokenKind compoundOp) => compoundOp switch
    {
        TokenKind.AddAssign      => TokenKind.Add,
        TokenKind.SubtractAssign => TokenKind.Subtract,
        TokenKind.MultiplyAssign => TokenKind.Multiply,
        TokenKind.DivideAssign   => TokenKind.DivideBy,
        TokenKind.ModuloAssign   => TokenKind.Modulo,
        TokenKind.PowerAssign    => TokenKind.Power,
        TokenKind.ConcatAppend   => TokenKind.Concat,
        TokenKind.NullCoalesceAssign => TokenKind.NullCoalesce,
        _ => throw new ArgumentException()
    };

    /// <summary>
    /// Returns the CLR operand type used when emitting a given binary operator.
    /// </summary>
    /// <param name="op">The <see cref="TokenKind"/> of the operator.</param>
    /// <returns>
    /// <see cref="double"/> for division, <see cref="string"/> for concatenation,
    /// and <see cref="int"/> for all other operators.
    /// </returns>
    private Type GetTypeForOp(TokenKind op) => op switch
    {
        TokenKind.DivideBy => typeof(double),
        TokenKind.Concat   => typeof(string),
        _                  => typeof(int)
    };

    /// <summary>
    /// Emits the IL opcode or helper call corresponding to a simple binary operator,
    /// assuming operands of the appropriate type are already on the stack.
    /// </summary>
    /// <param name="op">The <see cref="TokenKind"/> of the operator to emit.</param>
    /// <exception cref="NotImplementedException">Thrown for unrecognised operators.</exception>
    /// <remarks>
    /// <see cref="TokenKind.Power"/> delegates to <c>Math.Pow(double, double)</c> via
    /// <see cref="OpCodes.Call"/>. <see cref="TokenKind.Concat"/> calls
    /// <see cref="StringConcat"/>.
    /// </remarks>
    private void EmitOperator(TokenKind op)
    {
        switch (op)
        {
            case TokenKind.Add:      Emit(OpCodes.Add); break;
            case TokenKind.Subtract: Emit(OpCodes.Sub); break;
            case TokenKind.Multiply: Emit(OpCodes.Mul); break;
            case TokenKind.DivideBy: Emit(OpCodes.Div); break;
            case TokenKind.Modulo:   Emit(OpCodes.Rem); break;
            case TokenKind.Power:
                Emit(OpCodes.Call, typeof(Math).GetMethod("Pow", new[] { typeof(double), typeof(double) })!);
                break;
            case TokenKind.Concat:
                Emit(OpCodes.Call, StringConcat);
                break;
            default: throw new NotImplementedException();
        }
    }
}