using System.Reflection.Emit;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.Runtime.Types;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public class ILVisitor : IVisitor
{
    private readonly RuntimeContext _context = new();
    private readonly DynamicMethod _mainMethod;
    private readonly ILGenerator _il;

    // PhpValue constructors
    private static readonly System.Reflection.ConstructorInfo PhpValueObjectCtor =
        typeof(PhpValue).GetConstructor(new[] { typeof(object) })!;
    private static readonly System.Reflection.ConstructorInfo PhpValueIntCtor =
        typeof(PhpValue).GetConstructor(new[] { typeof(int) })!;
    private static readonly System.Reflection.ConstructorInfo PhpValueStringCtor =
        typeof(PhpValue).GetConstructor(new[] { typeof(string) })!;
    private static readonly System.Reflection.ConstructorInfo PhpValueBoolCtor =
        typeof(PhpValue).GetConstructor(new[] { typeof(bool) })!;

    // PhpValue operators / methods
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

    private readonly List<string> _ilLog = new();
    public IReadOnlyList<string> ILLog => _ilLog;

    private void Emit(OpCode op) { _ilLog.Add($"{op}"); _il.Emit(op); }
    private void Emit(OpCode op, int arg) { _ilLog.Add($"{op} {arg}"); _il.Emit(op, arg); }
    private void Emit(OpCode op, string arg) { _ilLog.Add($"{op} \"{arg}\""); _il.Emit(op, arg); }
    private void Emit(OpCode op, Type arg) { _ilLog.Add($"{op} {arg.Name}"); _il.Emit(op, arg); }
    private void Emit(OpCode op, System.Reflection.MethodInfo arg) { _ilLog.Add($"{op} {arg.DeclaringType?.Name}.{arg.Name}"); _il.Emit(op, arg); }
    private void Emit(OpCode op, System.Reflection.ConstructorInfo arg) { _ilLog.Add($"{op} {arg.DeclaringType?.Name}..ctor({string.Join(",", arg.GetParameters().Select(p => p.ParameterType.Name))})"); _il.Emit(op, arg); }
    private void EmitLabel(Label label) { _ilLog.Add($"label_{label.GetHashCode() & 0xFFFF}:"); _il.MarkLabel(label); }

    public ILVisitor()
    {
        _mainMethod = new DynamicMethod(
            "phpil_main",
            typeof(object),
            Type.EmptyTypes,
            typeof(ILVisitor).Module);

        _il = _mainMethod.GetILGenerator();
        _context.PushFrame(new StackFrame());
    }
    
    public ILVisitor(RuntimeContext? context = null, ILGenerator? il = null, List<string>? log = null)
    {
        _context = context ?? new RuntimeContext();
        _ilLog = log ?? new List<string>();

        if (il != null)
        {
            _mainMethod = null!;
            _il = il;
        }
        else
        {
            _mainMethod = new DynamicMethod(
                "phpil_main",
                typeof(object),
                Type.EmptyTypes,
                typeof(ILVisitor).Module);
            _il = _mainMethod.GetILGenerator();
        }

        _context.PushFrame(new StackFrame());
    }

    public object? Execute()
    {
        Emit(OpCodes.Ldnull);
        Emit(OpCodes.Ret);
        return _mainMethod.Invoke(null, null);
    }

    public string DumpIL() => string.Join("\n", _ilLog);

    public void VisitBlockNode(BlockNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
        {
            _ilLog.Add($"; --- {stmt.GetType().Name} ---");
            stmt.Accept(this, source);
            Emit(OpCodes.Pop);
        }
    }

    public void VisitIfNode(IfNode node, in ReadOnlySpan<char> source)
    {
        // All branches jump to this label when done
        var endLabel = _il.DefineLabel();

        // --- IF ---
        _ilLog.Add("; if condition");
        node.Expression?.Accept(this, source); // PhpValue on stack
        Emit(OpCodes.Callvirt, PhpValueToBool);  // bool on stack

        // If false, jump to first elseif (or else, or end)
        var nextLabel = _il.DefineLabel();
        _ilLog.Add($"brfalse next");
        _il.Emit(OpCodes.Brfalse, nextLabel);

        _ilLog.Add("; if body");
        if (node.Body != null)
        {
            foreach (var stmt in node.Body.Statements)
            {
                stmt.Accept(this, source);
                Emit(OpCodes.Pop);
            }
        }
        _ilLog.Add($"br end");
        _il.Emit(OpCodes.Br, endLabel);
        EmitLabel(nextLabel);

        // --- ELSEIF CHAIN ---
        foreach (var elseIf in node.ElseIfs)
        {
            _ilLog.Add("; elseif condition");
            elseIf.Expression?.Accept(this, source);
            Emit(OpCodes.Callvirt, PhpValueToBool);

            var elseIfNext = _il.DefineLabel();
            _ilLog.Add($"brfalse next");
            _il.Emit(OpCodes.Brfalse, elseIfNext);

            _ilLog.Add("; elseif body");
            if (elseIf.Body != null)
            {
                foreach (var stmt in elseIf.Body.Statements)
                {
                    stmt.Accept(this, source);
                    Emit(OpCodes.Pop);
                }
            }
            _ilLog.Add($"br end");
            _il.Emit(OpCodes.Br, endLabel);
            EmitLabel(elseIfNext);
        }

        // --- ELSE ---
        if (node.ElseNode != null)
        {
            _ilLog.Add("; else body");
            foreach (var stmt in ((BlockNode) node.ElseNode).Statements)
            {
                stmt.Accept(this, source);
                Emit(OpCodes.Pop);
            }
        }

        EmitLabel(endLabel);

        // if/else is a statement — push PhpValue.Null so BlockNode can pop uniformly
        _il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
        _ilLog.Add("ldsfld PhpValue.Null ; if statement result");
    }

    public void VisitElseIfNode(ElseIfNode node, in ReadOnlySpan<char> source)
    {
        // Handled inline by VisitIfNode — should never be called directly
        throw new InvalidOperationException("ElseIfNode should be handled by VisitIfNode");
    }

    public void VisitElseNode(ElseNode node, in ReadOnlySpan<char> source)
    {
        // Handled inline by VisitIfNode — should never be called directly
        throw new InvalidOperationException("ElseNode should be handled by VisitIfNode");
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
                _il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
                _ilLog.Add("ldsfld PhpValue.Null");
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
            _il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
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
            // Arithmetic — PhpValue op PhpValue → PhpValue
            case TokenKind.Add:      Emit(OpCodes.Call, PhpValueAdd); break;
            case TokenKind.Subtract: Emit(OpCodes.Call, PhpValueSub); break;
            case TokenKind.Multiply: Emit(OpCodes.Call, PhpValueMul); break;
            case TokenKind.DivideBy: Emit(OpCodes.Call, PhpValueDiv); break;
            case TokenKind.Concat:   Emit(OpCodes.Call, PhpValueConcat); break;

            // Comparison — op_X returns bool, wrap in PhpValue(bool)
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

            // Equality — return PhpValue directly
            case TokenKind.ShallowEquality:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("LooseEquals", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                break;
            case TokenKind.DeepEquality:
                Emit(OpCodes.Call, typeof(PhpValue).GetMethod("StrictEquals", new[] { typeof(PhpValue), typeof(PhpValue) })!);
                break;

            // Logical — return PhpValue directly
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
        _il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
    }

    public void VisitPostfixExpressionNode(PostfixExpressionNode node, in ReadOnlySpan<char> source)
    {
        if (node.Operand is VariableNode varNode)
        {
            string varName = varNode.Token.TextValue(source);
            if (_context.TryGetVariableSlot(varName, out int slot))
            {
                _ilLog.Add($"ldloc local_{slot} ; {varName} (original, stays on stack)");
                _il.Emit(OpCodes.Ldloc, slot);
                _ilLog.Add($"ldloc local_{slot} ; {varName} (copy to mutate)");
                _il.Emit(OpCodes.Ldloc, slot);
                _il.Emit(OpCodes.Ldc_I4_1);
                Emit(OpCodes.Newobj, PhpValueIntCtor);
                Emit(OpCodes.Call, node.Operator.Kind == TokenKind.Increment ? PhpValueAdd : PhpValueSub);
                _ilLog.Add($"stloc local_{slot} ; {varName}");
                _il.Emit(OpCodes.Stloc, slot);
                return;
            }
        }
        _il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
    }

    public void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source)
    {
        // 1. Load function name — identifier or variable
        if (node.Callee is IdentifierNode idNode)
        {
            Emit(OpCodes.Ldstr, idNode.Token.TextValue(source));
        }
        else if (node.Callee is VariableNode calleeVar)
        {
            // $callable(...) — load the PhpValue, extract the stored function name
            string varName = calleeVar.Token.TextValue(source);
            if (_context.TryGetVariableSlot(varName, out int varSlot))
            {
                _ilLog.Add($"ldloc local_{varSlot} ; {varName} (callable)");
                _il.Emit(OpCodes.Ldloc, varSlot);
            }
            else
            {
                _ilLog.Add($"; WARNING: callable {varName} not found");
                _il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
            }
            _il.Emit(OpCodes.Callvirt, typeof(PhpValue).GetMethod("ToStringValue")!);
            _ilLog.Add("callvirt PhpValue.ToStringValue ; resolve callable name");
        }
        else if (node.Callee != null)
        {
            node.Callee.Accept(this, source);
            Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString")!);
        }

        // 2. Build PhpValue[] args
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

        // 3. Dispatch
        Emit(OpCodes.Call, typeof(GlobalRuntimeContext).GetMethod("CallFunction",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!);
    }

    public void VisitVariableDeclaration(VariableDeclaration node, in ReadOnlySpan<char> source)
    {
        string varName = node.VariableName.TextValue(source);
        if (node.VariableValue != null) node.VariableValue.Accept(this, source);
        else _il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);

        if (!_context.TryGetVariableSlot(varName, out int slot))
            slot = _context.RegisterVariable(varName, _il);

        _il.Emit(OpCodes.Dup);
        _ilLog.Add($"stloc local_{slot} ; {varName}");
        _il.Emit(OpCodes.Stloc, slot);
    }

    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span) { }

    public void VisitArgumentListNode(ArgumentListNode node, in ReadOnlySpan<char> source)
    {
        Emit(OpCodes.Ldc_I4, node.Arguments.Count);
        Emit(OpCodes.Newarr, typeof(PhpValue));
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            Emit(OpCodes.Dup);
            Emit(OpCodes.Ldc_I4, i);
            node.Arguments[i].Accept(this, source);
            Emit(OpCodes.Stelem_Ref);
        }
    }

    public void VisitBreakNode(BreakNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitUnaryOpNode(UnaryOpNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitTernaryNode(TernaryNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitSyntaxNode(SyntaxNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitReturnNode(ReturnNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitIdentifierNode(IdentifierNode node, in ReadOnlySpan<char> source) 
    {
            
    }
    public void VisitGroupNode(GroupNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitExpressionNode(ExpressionNode node, in ReadOnlySpan<char> source)
    {
        foreach (var stmt in node.Statements)
            stmt.Accept(this, source);
    }
    public void VisitWhileNode(WhileNode node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitForNode(For node, in ReadOnlySpan<char> source) => throw new NotImplementedException();
    public void VisitFunctionNode(FunctionNode node, in ReadOnlySpan<char> source)
    {
        string funcName = node.Name.TextValue(source);

        var method = new DynamicMethod(
            $"phpil_{funcName}",
            typeof(PhpValue),
            new[] { typeof(PhpValue[]) },
            typeof(ILVisitor).Module);

        var funcContext = new RuntimeContext();
        var funcIl = method.GetILGenerator();
        var funcVisitor = new ILVisitor(funcContext, funcIl, _ilLog);

        // Unpack params from args array into locals
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

        // Compile body
        if (node.Body != null)
            foreach (var stmt in node.Body.Statements)
            {
                stmt.Accept(funcVisitor, source);
                funcIl.Emit(OpCodes.Pop);
            }

        // Default return PhpValue.Null
        funcIl.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
        funcIl.Emit(OpCodes.Ret);

        // Register
        GlobalRuntimeContext.FunctionTable[funcName] = new PhpFunction
        {
            Name = funcName,
            IsSystem = false,
            IsCompiled = true,
            Action = (PhpCallable)method.CreateDelegate(typeof(PhpCallable))
        };

        // Statement result for BlockNode to pop
        _il.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
        _ilLog.Add($"ldsfld PhpValue.Null ; fn decl {funcName}");
    }

    public void VisitFunctionParameter(FunctionParameter node, in ReadOnlySpan<char> source) => throw new NotImplementedException();

    public void VisitAnonymousFunctionNode(AnonymousFunctionNode node, in ReadOnlySpan<char> source)
    {
        // Generate a unique internal name
        string funcName = $"phpil_anon_{Guid.NewGuid():N}";

        var method = new DynamicMethod(
            funcName,
            typeof(PhpValue),
            new[] { typeof(PhpValue[]) },
            typeof(ILVisitor).Module);

        var funcContext = new RuntimeContext();
        var funcIl = method.GetILGenerator();
        var funcVisitor = new ILVisitor(funcContext, funcIl, _ilLog);

        // Unpack params
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

        // Compile body
        if (node.Body != null)
            foreach (var stmt in node.Body.Statements)
            {
                stmt.Accept(funcVisitor, source);
                funcIl.Emit(OpCodes.Pop);
            }

        // Default return
        funcIl.Emit(OpCodes.Ldsfld, typeof(PhpValue).GetField("Null")!);
        funcIl.Emit(OpCodes.Ret);

        // Register in function table
        var compiled = (PhpCallable)method.CreateDelegate(typeof(PhpCallable));
        GlobalRuntimeContext.FunctionTable[funcName] = new PhpFunction
        {
            Name = funcName,
            IsSystem = false,
            IsCompiled = true,
            Action = compiled
        };

        // Push a PhpValue(string) containing the function name so it can be
        // stored in a variable and later looked up via CallFunction
        _il.Emit(OpCodes.Ldstr, funcName);
        _il.Emit(OpCodes.Newobj, typeof(PhpValue).GetConstructor(new[] { typeof(string) })!);
        _ilLog.Add($"ldstr \"{funcName}\" / newobj PhpValue(string) ; anon fn ref");
    }
}