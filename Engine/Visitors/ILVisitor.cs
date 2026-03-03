using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public class ILVisitor : IVisitor
{
    private readonly RuntimeContext _context;
    private readonly DynamicMethod? _mainMethod;
    private readonly ILGenerator _il;
    private readonly List<string> _ilLog;

    private static readonly System.Reflection.ConstructorInfo PhpValueObjectCtor =
        typeof(PhpValue).GetConstructor(new[] { typeof(object) })!;
    private static readonly System.Reflection.ConstructorInfo PhpValueIntCtor =
        typeof(PhpValue).GetConstructor(new[] { typeof(int) })!;
    private static readonly System.Reflection.ConstructorInfo PhpValueStringCtor =
        typeof(PhpValue).GetConstructor(new[] { typeof(string) })!;
    private static readonly System.Reflection.ConstructorInfo PhpValueBoolCtor =
        typeof(PhpValue).GetConstructor(new[] { typeof(bool) })!;

    private static readonly System.Reflection.MethodInfo PhpValueAdd =
        typeof(PhpValue).GetMethod("op_Addition", new[] { typeof(PhpValue), typeof(PhpValue) })!;
    private static readonly System.Reflection.MethodInfo PhpValueSub =
        typeof(PhpValue).GetMethod("op_Subtraction", new[] { typeof(PhpValue), typeof(PhpValue) })!;
    private static readonly System.Reflection.MethodInfo PhpValueMul =
        typeof(PhpValue).GetMethod("op_Multiply", new[] { typeof(PhpValue), typeof(PhpValue) })!;
    private static readonly System.Reflection.MethodInfo PhpValueDiv =
        typeof(PhpValue).GetMethod("op_Division", new[] { typeof(PhpValue), typeof(PhpValue) })!;
    private static readonly System.Reflection.MethodInfo PhpValueConcat =
        typeof(PhpValue).GetMethod("Concat", new[] { typeof(PhpValue), typeof(PhpValue) })!;
    private static readonly System.Reflection.MethodInfo PhpValueToBool =
        typeof(PhpValue).GetMethod("ToBool")!;

    public IReadOnlyList<string> ILLog => _ilLog;

    // Root constructor
    public ILVisitor()
    {
        _context = new RuntimeContext();
        _ilLog = new List<string>();
        _mainMethod = new DynamicMethod(
            "phpil_main",
            typeof(object),
            Type.EmptyTypes,
            typeof(ILVisitor).Module);
        _il = _mainMethod.GetILGenerator();
        _context.PushFrame(new StackFrame());
    }

    // Scoped constructor for compiling function bodies
    public ILVisitor(RuntimeContext context, ILGenerator il, List<string> log)
    {
        _context = context;
        _il = il;
        _ilLog = log;
        _mainMethod = null;
        _context.PushFrame(new StackFrame());
    }

    public object? Execute()
    {
        Emit(OpCodes.Ldnull);
        Emit(OpCodes.Ret);
        return _mainMethod!.Invoke(null, null);
    }

    public string DumpIL() => string.Join("\n", _ilLog);

    // --- Emit helpers ---
    private void Emit(OpCode op) { _ilLog.Add($"{op}"); _il.Emit(op); }
    private void Emit(OpCode op, int arg) { _ilLog.Add($"{op} {arg}"); _il.Emit(op, arg); }
    private void Emit(OpCode op, string arg) { _ilLog.Add($"{op} \"{arg}\""); _il.Emit(op, arg); }
    private void Emit(OpCode op, Type arg) { _ilLog.Add($"{op} {arg.Name}"); _il.Emit(op, arg); }
    private void Emit(OpCode op, System.Reflection.MethodInfo arg) { _ilLog.Add($"{op} {arg.DeclaringType?.Name}.{arg.Name}"); _il.Emit(op, arg); }
    private void Emit(OpCode op, System.Reflection.ConstructorInfo arg) { _ilLog.Add($"{op} {arg.DeclaringType?.Name}..ctor({string.Join(",", arg.GetParameters().Select(p => p.ParameterType.Name))})"); _il.Emit(op, arg); }
    private void EmitLabel(Label label) { _ilLog.Add($"label_{label.GetHashCode() & 0xFFFF}:"); _il.MarkLabel(label); }
    private void EmitLoadNull() { _il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!); _ilLog.Add("ldsfld PhpValue.Null"); }
    public void EmitPop() => Emit(OpCodes.Pop);

    // --- Visitors ---

    public void VisitBlockNode(BlockNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
        {
            _ilLog.Add($"; --- {stmt.GetType().Name} ---");
            stmt.Accept(this, source);
            Emit(OpCodes.Pop);
        }
    }

    public void VisitLiteralNode(LiteralNode node, in ReadOnlySpan<char> source)
    {
        string value = node.Token.TextValue(source);
        switch (node.Token.Kind)
        {
            case TokenKind.IntLiteral:
                if (int.TryParse(value, out int intVal)) Emit(OpCodes.Ldc_I4, intVal);
                else Emit(OpCodes.Ldc_I4_0);
                Emit(OpCodes.Newobj, PhpValueIntCtor);
                break;
            case TokenKind.StringLiteral:
                string content = value.Length >= 2 ? value[1..^1] : value;
                Emit(OpCodes.Ldstr, content);
                Emit(OpCodes.Newobj, PhpValueStringCtor);
                break;
            case TokenKind.TrueLiteral:
                Emit(OpCodes.Ldc_I4_1);
                Emit(OpCodes.Newobj, PhpValueBoolCtor);
                break;
            case TokenKind.FalseLiteral:
                Emit(OpCodes.Ldc_I4_0);
                Emit(OpCodes.Newobj, PhpValueBoolCtor);
                break;
            default:
                EmitLoadNull();
                break;
        }
    }

    public void VisitVariableNode(VariableNode node, in ReadOnlySpan<char> source)
    {
        string varName = node.Token.TextValue(source);
        if (_context.TryGetVariableSlot(varName, out int slot))
        {
            _ilLog.Add($"ldloc local_{slot} ; {varName} (PhpValue)");
            _il.Emit(OpCodes.Ldloc, slot);
        }
        else
        {
            _ilLog.Add($"; WARNING: {varName} not found — loading PhpValue.Null");
            EmitLoadNull();
        }
    }

    public void VisitBinaryOpNode(BinaryOpNode node, in ReadOnlySpan<char> source)
    {
        _ilLog.Add($"; BinaryOp operator = {node.Operator}");

        if (node.Operator == TokenKind.AssignEquals)
        {
            if (node.Left is VariableNode varNode)
            {
                string varName = varNode.Token.TextValue(source);
                node.Right?.Accept(this, source);
                if (!_context.TryGetVariableSlot(varName, out int slot))
                    slot = _context.RegisterVariable(varName, _il);
                _il.Emit(OpCodes.Dup);
                _ilLog.Add($"stloc local_{slot} ; {varName}");
                _il.Emit(OpCodes.Stloc, slot);
            }
            return;
        }

        node.Left?.Accept(this, source);
        node.Right?.Accept(this, source);

        switch (node.Operator)
        {
            case TokenKind.Add:      Emit(OpCodes.Call, PhpValueAdd); break;
            case TokenKind.Subtract: Emit(OpCodes.Call, PhpValueSub); break;
            case TokenKind.Multiply: Emit(OpCodes.Call, PhpValueMul); break;
            case TokenKind.DivideBy: Emit(OpCodes.Call, PhpValueDiv); break;
            case TokenKind.Concat:   Emit(OpCodes.Call, PhpValueConcat); break;
            case TokenKind.LessThan:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("op_LessThan", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                Emit(OpCodes.Newobj, PhpValueBoolCtor);
                break;
            case TokenKind.GreaterThan:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("op_GreaterThan", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                Emit(OpCodes.Newobj, PhpValueBoolCtor);
                break;
            case TokenKind.LessThanOrEqual:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("op_LessThanOrEqual", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                Emit(OpCodes.Newobj, PhpValueBoolCtor);
                break;
            case TokenKind.GreaterThanOrEqual:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("op_GreaterThanOrEqual", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                Emit(OpCodes.Newobj, PhpValueBoolCtor);
                break;
            case TokenKind.ShallowEquality:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("LooseEquals", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                break;
            case TokenKind.DeepEquality:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("StrictEquals", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                break;
            case TokenKind.LogicalAnd:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("And", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                break;
            case TokenKind.LogicalOr:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("Or", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                break;
        }
    }

    public void VisitPrefixExpressionNode(PrefixExpressionNode node, in ReadOnlySpan<char> source)
    {
        if (node.Operand is VariableNode varNode)
        {
            string varName = varNode.Token.TextValue(source);
            if (_context.TryGetVariableSlot(varName, out int slot))
            {
                _ilLog.Add($"ldloc local_{slot} ; {varName}");
                _il.Emit(OpCodes.Ldloc, slot);
                _il.Emit(OpCodes.Ldc_I4_1);
                Emit(OpCodes.Newobj, PhpValueIntCtor);
                Emit(OpCodes.Call, node.Operator.Kind == TokenKind.Increment ? PhpValueAdd : PhpValueSub);
                _il.Emit(OpCodes.Dup);
                _ilLog.Add($"stloc local_{slot} ; {varName}");
                _il.Emit(OpCodes.Stloc, slot);
                return;
            }
        }
        EmitLoadNull();
    }

    public void VisitPostfixExpressionNode(PostfixExpressionNode node, in ReadOnlySpan<char> source)
    {
        if (node.Operand is VariableNode varNode)
        {
            string varName = varNode.Token.TextValue(source);
            if (_context.TryGetVariableSlot(varName, out int slot))
            {
                _il.Emit(OpCodes.Ldloc, slot); // original (stays on stack)

                _il.Emit(OpCodes.Ldloc, slot); // copy to mutate
                _il.Emit(OpCodes.Ldc_I4_1);    // <-- FIX
                Emit(OpCodes.Newobj, PhpValueIntCtor);
                Emit(OpCodes.Call, node.Operator.Kind == TokenKind.Increment ? PhpValueAdd : PhpValueSub);

                _il.Emit(OpCodes.Stloc, slot);
                return;
            }
        }

        EmitLoadNull();
    }

    public void VisitIfNode(IfNode node, in ReadOnlySpan<char> source)
    {
        var endLabel = _il.DefineLabel();

        _ilLog.Add("; if condition");
        node.Expression?.Accept(this, source);
        Emit(OpCodes.Callvirt, PhpValueToBool);

        var nextLabel = _il.DefineLabel();
        _ilLog.Add("brfalse next");
        _il.Emit(OpCodes.Brfalse, nextLabel);

        _ilLog.Add("; if body");
        if (node.Body != null)
            foreach (var stmt in node.Body.Statements)
            {
                stmt.Accept(this, source);
                Emit(OpCodes.Pop);
            }

        _ilLog.Add("br end");
        _il.Emit(OpCodes.Br, endLabel);
        EmitLabel(nextLabel);

        foreach (var elseIf in node.ElseIfs)
        {
            _ilLog.Add("; elseif condition");
            elseIf.Expression?.Accept(this, source);
            Emit(OpCodes.Callvirt, PhpValueToBool);

            var elseIfNext = _il.DefineLabel();
            _ilLog.Add("brfalse next");
            _il.Emit(OpCodes.Brfalse, elseIfNext);

            _ilLog.Add("; elseif body");
            if (elseIf.Body != null)
                foreach (var stmt in elseIf.Body.Statements)
                {
                    stmt.Accept(this, source);
                    Emit(OpCodes.Pop);
                }

            _ilLog.Add("br end");
            _il.Emit(OpCodes.Br, endLabel);
            EmitLabel(elseIfNext);
        }

        if (node.ElseNode != null)
        {
            _ilLog.Add("; else body");
            foreach (var stmt in ((BlockNode)node.ElseNode).Statements)
            {
                stmt.Accept(this, source);
                Emit(OpCodes.Pop);
            }
        }

        EmitLabel(endLabel);
        EmitLoadNull();
        _ilLog.Add("; if statement result");
    }

    public void VisitElseIfNode(ElseIfNode node, in ReadOnlySpan<char> source)
        => throw new InvalidOperationException("ElseIfNode handled inline by VisitIfNode");

    public void VisitElseNode(ElseNode node, in ReadOnlySpan<char> source)
        => throw new InvalidOperationException("ElseNode handled inline by VisitIfNode");

    public void VisitWhileNode(WhileNode node, in ReadOnlySpan<char> source)
    {
        var loopStart = _il.DefineLabel();
        var loopEnd   = _il.DefineLabel();

        EmitLabel(loopStart);

        _ilLog.Add("; while condition");
        node.Expression?.Accept(this, source);
        Emit(OpCodes.Callvirt, PhpValueToBool);
        _ilLog.Add("brfalse end");
        _il.Emit(OpCodes.Brfalse, loopEnd);

        _ilLog.Add("; while body");
        if (node.Body != null)
            foreach (var stmt in node.Body.Statements)
            {
                stmt.Accept(this, source);
                Emit(OpCodes.Pop);
            }

        _ilLog.Add("br loop_start");
        _il.Emit(OpCodes.Br, loopStart);

        EmitLabel(loopEnd);
        EmitLoadNull();
        _ilLog.Add("; while result");
    }

    public void VisitFunctionNode(FunctionNode node, in ReadOnlySpan<char> source)
    {
        string funcName = node.Name.TextValue(source);
        _ilLog.Add($"; --- FunctionNode: {funcName} ---");

        var method = new DynamicMethod(
            $"phpil_{funcName}",
            typeof(PhpValue),
            new[] { typeof(PhpValue[]) },
            typeof(ILVisitor).Module);

        var funcContext = new RuntimeContext();
        var funcIl = method.GetILGenerator();
        var funcVisitor = new ILVisitor(funcContext, funcIl, _ilLog);

        for (int i = 0; i < node.Params.Count; i++)
        {
            string paramName = node.Params[i].Name.TextValue(source);
            int slot = funcContext.RegisterVariable(paramName, funcIl);
            funcIl.Emit(OpCodes.Ldarg_0);
            funcIl.Emit(OpCodes.Ldc_I4, i);
            funcIl.Emit(OpCodes.Ldelem_Ref);
            funcIl.Emit(OpCodes.Stloc, slot);
            _ilLog.Add($"; param[{i}] {paramName} -> local_{slot}");
        }

        if (node.Body != null)
            foreach (var stmt in node.Body.Statements)
            {
                _ilLog.Add($"; [fn:{funcName}] --- {stmt.GetType().Name} ---");
                stmt.Accept(funcVisitor, source);
                funcVisitor.EmitPop();
            }

        funcIl.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
        funcIl.Emit(OpCodes.Ret);

        GlobalRuntimeContext.FunctionTable[funcName] = new PhpFunction
        {
            Name = funcName,
            IsSystem = false,
            IsCompiled = true,
            Action = (PhpCallable)method.CreateDelegate(typeof(PhpCallable))
        };

        EmitLoadNull();
        _ilLog.Add($"; fn decl {funcName}");
    }

    public void VisitAnonymousFunctionNode(AnonymousFunctionNode node, in ReadOnlySpan<char> source)
    {
        string funcName = $"phpil_anon_{Guid.NewGuid():N}";
        _ilLog.Add($"; --- AnonymousFunctionNode: {funcName} ---");

        var method = new DynamicMethod(
            funcName,
            typeof(PhpValue),
            new[] { typeof(PhpValue[]) },
            typeof(ILVisitor).Module);

        var funcContext = new RuntimeContext();
        var funcIl = method.GetILGenerator();
        var funcVisitor = new ILVisitor(funcContext, funcIl, _ilLog);

        for (int i = 0; i < node.Params.Count; i++)
        {
            string paramName = node.Params[i].Name.TextValue(source);
            int slot = funcContext.RegisterVariable(paramName, funcIl);
            funcIl.Emit(OpCodes.Ldarg_0);
            funcIl.Emit(OpCodes.Ldc_I4, i);
            funcIl.Emit(OpCodes.Ldelem_Ref);
            funcIl.Emit(OpCodes.Stloc, slot);
            _ilLog.Add($"; anon param[{i}] {paramName} -> local_{slot}");
        }

        if (node.Body != null)
            foreach (var stmt in node.Body.Statements)
            {
                _ilLog.Add($"; [anon] --- {stmt.GetType().Name} ---");
                stmt.Accept(funcVisitor, source);
                funcVisitor.EmitPop();
            }

        funcIl.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
        funcIl.Emit(OpCodes.Ret);

        GlobalRuntimeContext.FunctionTable[funcName] = new PhpFunction
        {
            Name = funcName,
            IsSystem = false,
            IsCompiled = true,
            Action = (PhpCallable)method.CreateDelegate(typeof(PhpCallable))
        };

        // Push function name as PhpValue(string) so it can be stored in a variable
        _il.Emit(OpCodes.Ldstr, funcName);
        _il.Emit(OpCodes.Newobj, PhpValueStringCtor);
        _ilLog.Add($"ldstr / newobj PhpValue(string) ; anon fn ref -> {funcName}");
    }

    public void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source)
    {
        if (node.Callee is IdentifierNode idNode)
        {
            Emit(OpCodes.Ldstr, idNode.Token.TextValue(source));
        }
        else if (node.Callee is VariableNode calleeVar)
        {
            string varName = calleeVar.Token.TextValue(source);
            if (_context.TryGetVariableSlot(varName, out int varSlot))
            {
                _ilLog.Add($"ldloc local_{varSlot} ; {varName} (callable)");
                _il.Emit(OpCodes.Ldloc, varSlot);
            }
            else
            {
                _ilLog.Add($"; WARNING: callable {varName} not found");
                EmitLoadNull();
            }
            _il.Emit(OpCodes.Callvirt, typeof(PhpValue).GetMethod("ToStringValue")!);
            _ilLog.Add("callvirt PhpValue.ToStringValue");
        }
        else if (node.Callee != null)
        {
            node.Callee.Accept(this, source);
            Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString")!);
        }

        Emit(OpCodes.Ldc_I4, node.Args.Count);
        Emit(OpCodes.Newarr, typeof(PhpValue));

        for (int i = 0; i < node.Args.Count; i++)
        {
            Emit(OpCodes.Dup);
            Emit(OpCodes.Ldc_I4, i);
            _ilLog.Add($"; arg[{i}]: {node.Args[i].GetType().Name}");
            node.Args[i].Accept(this, source);
            Emit(OpCodes.Stelem_Ref);
        }

        Emit(OpCodes.Call, typeof(GlobalRuntimeContext).GetMethod("CallFunction",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!);
    }

    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
        string varName = node.VariableName.TextValue(source);
        if (node.VariableValue != null) node.VariableValue.Accept(this, source);
        else EmitLoadNull();

        if (!_context.TryGetVariableSlot(varName, out int slot))
            slot = _context.RegisterVariable(varName, _il);

        _il.Emit(OpCodes.Dup);
        _ilLog.Add($"stloc local_{slot} ; {varName}");
        _il.Emit(OpCodes.Stloc, slot);
    }

    public void VisitFunctionParameter(FunctionParameter node, in ReadOnlySpan<char> source)
        => throw new NotImplementedException();

    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span) { }
    public void VisitArgumentListNode(ArgumentListNode node, in ReadOnlySpan<char> source) { }
    public void VisitBreakNode(BreakNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitSyntaxNode(SyntaxNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();

    public void VisitReturnNode(ReturnNode node, in ReadOnlySpan<char> source)
    {
        node.Expression?.Accept(this, source);
        Emit(OpCodes.Ret);
    }
    public void VisitIdentifierNode(IdentifierNode node, in ReadOnlySpan<char> source) { }
    public void VisitGroupNode(GroupNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitExpressionNode(ExpressionNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
            stmt.Accept(this, source);
    }
    public void VisitForNode(For node, in ReadOnlySpan<char> source)
    {
        var loopStart = _il.DefineLabel();
        var loopEnd   = _il.DefineLabel();

        // --- initializer ---
        if (node.Init != null)
        {
            node.Init.Accept(this, source);
            Emit(OpCodes.Pop); // discard result unless needed
        }

        // --- loop start ---
        EmitLabel(loopStart);

        // --- condition ---
        if (node.Condition != null)
        {
            node.Condition.Accept(this, source);
            Emit(OpCodes.Callvirt, PhpValueToBool);
            _il.Emit(OpCodes.Brfalse, loopEnd);
        }

        // --- loop body ---
        if (node.Body != null)
        {
            foreach (var stmt in (node.Body as BlockNode)!.Statements)
            {
                stmt.Accept(this, source);
                Emit(OpCodes.Pop);
            }
        }

        // --- increment ---
        if (node.Increment != null)
        {
            node.Increment.Accept(this, source);
            Emit(OpCodes.Pop);
        }

        // --- jump back to loop start ---
        _il.Emit(OpCodes.Br, loopStart);

        // --- loop end ---
        EmitLabel(loopEnd);
        EmitLoadNull();
        _ilLog.Add("; for loop result");
    }
}