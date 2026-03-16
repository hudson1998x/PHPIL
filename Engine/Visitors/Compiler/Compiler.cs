using System.Reflection.Emit;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler : IVisitor
{
    /// <summary>The namespace currently active during compilation, used to qualify type and function names.</summary>
    private string _currentNamespace = "";

    /// <summary>
    /// Maps use-import aliases to their fully-qualified names, populated by <see cref="VisitUseNode"/>.
    /// </summary>
    private readonly Dictionary<string, string> _useImports = [];

    /// <summary>
    /// The <see cref="Type"/> builder for the class currently being compiled, or
    /// <see langword="null"/> when compiling at file scope.
    /// </summary>
    private Type? _currentType;

    /// <summary>
    /// Indicates whether the method currently being compiled is static, used to suppress
    /// <c>$this</c> binding and adjust argument index offsets.
    /// </summary>
    private bool _isStaticMethod;

    /// <summary>
    /// Stack of exit labels for the currently active <c>if</c> chains, maintained in
    /// innermost-first order so that <c>elseif</c> bodies can branch to the correct exit point.
    /// </summary>
    private readonly Stack<Label> _exitLabels = new();

    /// <summary>
    /// Fallback visitor — not implemented; node types are dispatched via their typed overloads.
    /// </summary>
    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Emits IL for each argument in an argument list by visiting them in order.
    /// </summary>
    /// <param name="node">The <see cref="ArgumentListNode"/> containing the arguments.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    public void VisitArgumentListNode(ArgumentListNode node, in ReadOnlySpan<char> source)
    {
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this, source);
        }
    }

    /// <summary>
    /// Emits IL for an <c>elseif</c> clause, evaluating its condition and branching to the
    /// enclosing <c>if</c> statement's exit label when the body executes.
    /// </summary>
    /// <param name="node">The <see cref="ElseIfNode"/> representing the elseif clause.</param>
    /// <param name="source">The original source text, passed through to child node visitors.</param>
    /// <remarks>
    /// <para>
    /// A <c>falseLabel</c> is defined locally; if the condition is <see langword="false"/>,
    /// control branches to it, skipping the body. Condition unboxing varies by expression type:
    /// <see cref="FunctionCallNode"/> results are unboxed to <see cref="bool"/> and compared to
    /// <c>1</c>; <see cref="VariableNode"/> results are unboxed directly to <see cref="int"/>;
    /// all other expression results are assumed to already be in an appropriate form for
    /// <see cref="OpCodes.Brfalse"/>.
    /// </para>
    /// <para>
    /// After the body is emitted, an unconditional branch to <c>_exitLabels.Peek()</c> transfers
    /// control to the end of the parent <c>if</c> chain. The remainder of the chain
    /// (<c>elseif</c>/<c>else</c> siblings) is handled entirely by the enclosing
    /// <see cref="VisitIfNode"/>.
    /// </para>
    /// </remarks>
    public void VisitElseIfNode(ElseIfNode node, in ReadOnlySpan<char> source)
    {
        // ElseIfNode is a conditional in the chain - evaluate condition and execute body
        // Jump to parent's exit label when condition is true
        var falseLabel = DefineLabel();

        if (node.Expression != null)
        {
            node.Expression.Accept(this, source);
            // Properly handle different expression types for condition evaluation
            if (node.Expression is FunctionCallNode)
            {
                // isset() and similar functions return bool
                Emit(OpCodes.Unbox_Any, typeof(bool));
                // Convert bool to int (true→1, false→0) for Brfalse
                Emit(OpCodes.Ldc_I4_1);
                Emit(OpCodes.Ceq);  // Compare: if bool == 1, result is 1, else 0
            }
            else if (node.Expression is VariableNode)
            {
                // Variables may be boxed - unbox to int directly
                Emit(OpCodes.Unbox_Any, typeof(int));
            }
            // else: expression result is already in proper form

            Emit(OpCodes.Brfalse, falseLabel);
        }

        if (node.Body != null)
            node.Body.Accept(this, source);

        // When elseif body executes, jump to parent if's exit label
        if (_exitLabels.Count > 0)
            Emit(OpCodes.Br, _exitLabels.Peek());

        MarkLabel(falseLabel);
        // Note: Don't process ElseIfs and ElseNode here - parent IfNode handles the chain
    }

    /// <summary>Visitor stub — not yet implemented.</summary>
    public void VisitGroupNode(GroupNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Emits IL for a bare identifier, which in PHP is treated as a constant reference.
    /// Handles compile-time magic constants (__FILE__, __DIR__, __LINE__, __FUNCTION__, __METHOD__, __CLASS__).
    /// User-defined constants must be accessed via the constant() function at runtime.
    /// </summary>
    /// <param name="node">The <see cref="IdentifierNode"/> representing the identifier.</param>
    /// <param name="source">The original source text, used to resolve the constant name and calculate line numbers.</param>
    public void VisitIdentifierNode(IdentifierNode node, in ReadOnlySpan<char> source)
    {
        var identifierName = node.Token.TextValue(in source);

        // Check for magic constants (all compile-time, no runtime lookup needed)
        if (identifierName.Equals("__FILE__", StringComparison.OrdinalIgnoreCase))
        {
            Emit(OpCodes.Ldstr, _fileName);
            return;
        }

        if (identifierName.Equals("__DIR__", StringComparison.OrdinalIgnoreCase))
        {
            var dirPath = Path.GetDirectoryName(_fileName) ?? "";
            Emit(OpCodes.Ldstr, dirPath);
            return;
        }

        if (identifierName.Equals("__LINE__", StringComparison.OrdinalIgnoreCase))
        {
            // Calculate line number by counting newlines up to token position
            var lineNumber = 1;
            for (int i = 0; i < node.Token.RangeStart && i < source.Length; i++)
            {
                if (source[i] == '\n')
                    lineNumber++;
            }
            Emit(OpCodes.Ldc_I4, lineNumber);
            Emit(OpCodes.Box, typeof(int));
            return;
        }

        if (identifierName.Equals("__FUNCTION__", StringComparison.OrdinalIgnoreCase))
        {
            Emit(OpCodes.Ldstr, _currentFunctionName);
            return;
        }

        if (identifierName.Equals("__METHOD__", StringComparison.OrdinalIgnoreCase))
        {
            // Format: ClassName::methodName
            var methodFullName = string.IsNullOrEmpty(_currentMethodName) 
                ? "" 
                : (_currentType != null ? _currentType.Name.Replace(".", "\\") : "") + "::" + _currentMethodName;
            Emit(OpCodes.Ldstr, methodFullName);
            return;
        }

        if (identifierName.Equals("__CLASS__", StringComparison.OrdinalIgnoreCase))
        {
            var className = _currentType != null ? _currentType.Name.Replace(".", "\\") : "";
            Emit(OpCodes.Ldstr, className);
            return;
        }

        // For user-defined constants, users must use the constant() function at runtime
        // This avoids IL verification issues with reflection-based method calls
        throw new Exception($"Undefined constant: {identifierName}. Use constant('{identifierName}') to look up user-defined constants.");
    }

    /// <summary>
    /// Emits IL for an <c>else</c> clause body without any condition check.
    /// </summary>
    /// <param name="node">The <see cref="ElseNode"/> representing the else clause.</param>
    /// <param name="source">The original source text, passed through to the body visitor.</param>
    /// <remarks>
    /// No exit-label branch is emitted here — the enclosing <see cref="VisitIfNode"/> marks
    /// the exit label immediately after all <c>elseif</c> and <c>else</c> bodies have been
    /// processed.
    /// </remarks>
    public void VisitElseNode(ElseNode node, in ReadOnlySpan<char> source)
    {
        // ElseNode just executes its body without condition checking
        // No need to jump to exit label - the parent IfNode will mark it after all elseif/else are processed
        if (node.Body != null)
            node.Body.Accept(this, source);
    }

    /// <summary>Visitor stub — not yet implemented.</summary>
    public void VisitSyntaxNode(SyntaxNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Emits IL to compile a PHP anonymous function into a <see cref="Runtime.Closure"/> object
    /// and leave that closure on the stack.
    /// </summary>
    /// <param name="node">The <see cref="AnonymousFunctionNode"/> representing the anonymous function.</param>
    /// <param name="source">The original source text, used to resolve parameter names and the return type hint.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the underlying <see cref="System.Reflection.Emit.DynamicMethod"/> cannot be
    /// created, or when the <c>Closure(string)</c> constructor cannot be located via reflection.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Compilation mirrors <see cref="VisitFunctionNode"/>: a uniquely-named child
    /// <see cref="Compiler"/> is created, parameters are loaded from arguments into typed locals,
    /// the body is emitted, and an implicit <see langword="null"/> return is appended for
    /// non-<see langword="void"/> functions.
    /// </para>
    /// <para>
    /// The completed <see cref="System.Reflection.Emit.DynamicMethod"/> is baked into a delegate
    /// whose type is selected from the standard <see cref="Action"/> / <see cref="Func{TResult}"/>
    /// families based on the parameter count and return type. Functions with more than three
    /// parameters fall back to <see cref="MulticastDelegate"/>. The delegate is registered with
    /// <c>FunctionTable</c> under the generated name.
    /// </para>
    /// <para>
    /// Finally, the generated function name is pushed as a string and a <see cref="Runtime.Closure"/>
    /// is instantiated via <see cref="OpCodes.Newobj"/>, leaving the closure object on the stack
    /// as the expression result.
    /// </para>
    /// </remarks>
    public void VisitAnonymousFunctionNode(AnonymousFunctionNode node, in ReadOnlySpan<char> source)
    {
        var functionName = "anonymous_func_" + FunctionTable.GetNextAnonymousId();
        
        var parameterTypes = new Type[node.Params.Count];
        for (int i = 0; i < node.Params.Count; i++)
        {
            parameterTypes[i] = typeof(object); // Default to mixed for now
        }

        var returnType = typeof(object); // Default to mixed for now
        if (node.ReturnType != null)
        {
            returnType = node.ReturnType.Value.TextValue(in source) switch
            {
                "int" => typeof(int),
                "float" or "double" => typeof(double),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "void" => typeof(void),
                _ => typeof(object)
            };
        }

        var innerCompiler = new Compiler(functionName, returnType, parameterTypes);
        innerCompiler._currentNamespace = _currentNamespace;
        foreach (var import in _useImports)
            innerCompiler._useImports[import.Key] = import.Value;

        // Load parameters into locals
        for (int i = 0; i < node.Params.Count; i++)
        {
            var paramName = node.Params[i].Name.TextValue(in source);
            var local = innerCompiler.DeclareLocal(parameterTypes[i]);
            innerCompiler._locals[paramName] = local;

            // Emit Ldarg_i
            switch (i)
            {
                case 0: innerCompiler.Emit(OpCodes.Ldarg_0); break;
                case 1: innerCompiler.Emit(OpCodes.Ldarg_1); break;
                case 2: innerCompiler.Emit(OpCodes.Ldarg_2); break;
                case 3: innerCompiler.Emit(OpCodes.Ldarg_3); break;
                default: innerCompiler.Emit(OpCodes.Ldarg_S, (short)i); break;
            }

            innerCompiler.Emit(OpCodes.Stloc, local);
        }

        // Generate body
        if (node.Body != null)
        {
            node.Body.Accept(innerCompiler, source);
        }

        // Implicit return null if void/object
        if (returnType != typeof(void))
        {
            innerCompiler.Emit(OpCodes.Ldnull);
        }
        innerCompiler.Emit(OpCodes.Ret);

        // Get the dynamic method and create delegate
        var dynamicMethod = innerCompiler.GetDynamicMethod();
        if (dynamicMethod == null)
        {
            throw new InvalidOperationException("Failed to create dynamic method for anonymous function");
        }

        // Build delegate type
        Type delegateType;
        if (returnType == typeof(void))
        {
            switch (parameterTypes.Length)
            {
                case 0: delegateType = typeof(Action); break;
                case 1: delegateType = typeof(Action<>).MakeGenericType(parameterTypes[0]); break;
                case 2: delegateType = typeof(Action<,>).MakeGenericType(parameterTypes[0], parameterTypes[1]); break;
                case 3: delegateType = typeof(Action<,,>).MakeGenericType(parameterTypes[0], parameterTypes[1], parameterTypes[2]); break;
                default: delegateType = typeof(MulticastDelegate); break;
            }
        }
        else
        {
            switch (parameterTypes.Length)
            {
                case 0: delegateType = typeof(Func<>).MakeGenericType(returnType); break;
                case 1: delegateType = typeof(Func<,>).MakeGenericType(parameterTypes[0], returnType); break;
                case 2: delegateType = typeof(Func<,,>).MakeGenericType(parameterTypes[0], parameterTypes[1], returnType); break;
                case 3: delegateType = typeof(Func<,,,>).MakeGenericType(parameterTypes[0], parameterTypes[1], parameterTypes[2], returnType); break;
                default: delegateType = typeof(MulticastDelegate); break;
            }
        }

        // Create delegate from dynamic method
        var anonDel = dynamicMethod.CreateDelegate(delegateType);

        // Register the function
        var phpFunc = new PhpFunction
        {
            Name = functionName,
            ReturnType = returnType,
            ParameterTypes = parameterTypes,
            Method = anonDel
        };
        FunctionTable.RegisterFunction(phpFunc);

        // Emit Closure object with function name
        Emit(OpCodes.Ldstr, functionName);
        var ctor = typeof(PHPIL.Engine.Runtime.Closure).GetConstructor(new[] { typeof(string) });
        if (ctor == null) throw new InvalidOperationException("Closure constructor not found");
        Emit(OpCodes.Newobj, ctor);
    }
}