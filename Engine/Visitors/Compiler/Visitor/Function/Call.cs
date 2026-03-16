using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.Exceptions;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;
using PHPIL.Engine.SyntaxTree.Structure.OOP;
using PHPIL.Engine.Visitors.SemanticAnalysis;
using PHPIL.Engine.Runtime;
using PHPIL.Engine.Runtime.Diagnostics;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{

    private string _fileName = "vm:0";
    private string _currentFunctionName = "";
    private string _currentMethodName = "";

    public void WithFileName(string fileName)
    {
        _fileName = fileName;
    }
    
    /// <summary>
    /// Emits IL for a function or method call expression.
    /// </summary>
    /// <param name="node">The <see cref="FunctionCallNode"/> representing the call expression.</param>
    /// <param name="source">The original source text, used to resolve callee and argument names.</param>
    /// <exception cref="NotImplementedException">
    /// Thrown when the callee resolves to an unimplemented function, an unresolvable qualified name,
    /// or a static method call on a dynamic type.
    /// </exception>
    /// <remarks>
    /// Callee resolution is attempted in the following order:
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <b><c>isset()</c></b> — detected by name before any other dispatch and handled
    ///       entirely by <see cref="EmitIsset"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Instance method call</b> (<c>$obj->method(...)</c>) — the target object is pushed,
    ///       then the method is resolved via <c>TypeTable</c> for <c>$this</c> calls. If the method
    ///       is found, non-variadic, and has no spread arguments, a direct <see cref="OpCodes.Callvirt"/>
    ///       is emitted. Otherwise <c>RuntimeHelpers.CallMethod</c> or
    ///       <c>RuntimeHelpers.CallVariadicMethod</c> is used with an <c>object[]</c> argument array.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Static method call</b> (<c>ClassName::method(...)</c>) — the target type is resolved
    ///       via <c>TypeTable</c>, with <c>self</c> aliased to <c>_currentType</c>. If the type and
    ///       method are found and no spread arguments are present, a direct <see cref="OpCodes.Call"/>
    ///       is emitted. Otherwise <c>RuntimeHelpers.CallStaticMethodByName</c> is used.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Closure variable call</b> (<c>$callable(...)</c>) — the variable is loaded, arguments
    ///       are packed into an <c>object[]</c>, and <c>Closure.Invoke</c> is called via
    ///       <see cref="OpCodes.Callvirt"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Qualified name call</b> — resolved via <see cref="ResolveFunction"/>. File inclusion
    ///       functions (<c>require</c>, <c>require_once</c>, <c>include</c>, <c>include_once</c>) are
    ///       dispatched to <see cref="ExecuteIncludeFunction"/>; all others to
    ///       <see cref="ResolveParamsAndCall"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Regular named function call</b> — resolved via <see cref="ResolveFunction"/> and
    ///       dispatched to <see cref="ExecuteIncludeFunction"/> or <see cref="ResolveParamsAndCall"/>
    ///       as above.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public void VisitFunctionCallNode(FunctionCallNode node, in ReadOnlySpan<char> source)
    {
        // Check for isset() FIRST before anything else - try multiple detection methods
        string? callName = null;
        
        // Method 1: Direct IdentifierNode
        if (node.Callee is IdentifierNode issetIdNode)
        {
            callName = issetIdNode.Token.TextValue(in source);
        }
        // Method 2: Single-part QualifiedNameNode
        else if (node.Callee is QualifiedNameNode issetQname)
        {
            var parts = new List<string>();
            foreach (var p in issetQname.Parts)
                parts.Add(p.TextValue(in source));
            if (parts.Count == 1)
            {
                callName = parts[0];
            }
        }
        
        // Check if this is isset
        if (callName != null && callName.Equals("isset", StringComparison.OrdinalIgnoreCase))
        {
            EmitIsset(node, source);
            return;
        }
        
        // Try instance method call first
        if (node.Callee is ObjectAccessNode objAccess)
        {
            // Instance method call: $obj->method(...)
            objAccess.Object?.Accept(this, source); // Push object
            var methodName = objAccess.Property.Token.TextValue(in source);
            MethodInfo? method = null;
            
            // Check if it's $this
            if (objAccess.Object is VariableNode varNode && varNode.Token.TextValue(in source) == "$this" && _currentType != null)
            {
                // Try to get method from TypeTable
                var fqn = _currentType.Name.Replace(".", "\\");
                method = TypeTable.GetMethod(fqn, methodName);
            }
            
            bool hasSpread = node.Args.Any(arg => arg is SpreadNode);
            bool isVariadic = false;
            if (method != null)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(object[]))
                {
                    isVariadic = true;
                }

            }

            if (method != null && !hasSpread && !isVariadic)
            {
                // Direct call is safe
                foreach (var arg in node.Args) arg.Accept(this, source);
                Emit(OpCodes.Callvirt, method);
                return;
            }
            
            // Fall back to runtime helper for dynamic method calls or spread arguments or variadic methods
            Emit(OpCodes.Ldstr, methodName);
            var callMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod(isVariadic ? "CallVariadicMethod" : "CallMethod", new[] { typeof(object), typeof(string), typeof(object[]) })!;
            
            // Emit argument count
            Emit(OpCodes.Ldc_I4, node.Args.Count);
            Emit(OpCodes.Newarr, typeof(object));
            
            // Store args to array
            for (int i = 0; i < node.Args.Count; i++)
            {
                Emit(OpCodes.Dup);
                Emit(OpCodes.Ldc_I4, i);
                node.Args[i].Accept(this, source);
                EmitBoxingIfLiteral(node.Args[i]);
                Emit(OpCodes.Stelem_Ref);
            }
            
            Emit(OpCodes.Call, callMethod);
            return;
        }
        // Static method call
        else if (node.Callee is StaticAccessNode staticAccess)
        {
            string? methodName = null;
            if (staticAccess.MemberName is IdentifierNode idNode)
                methodName = idNode.Token.TextValue(in source);
            
            if (methodName == null)
                throw new Exception("Cannot resolve static method name");
                
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
            bool hasSpread = node.Args.Any(arg => arg is SpreadNode);

            if (targetType != null && !hasSpread)
            {
                var method = targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (method != null)
                {
                    foreach (var arg in node.Args) arg.Accept(this, source);
                    Emit(OpCodes.Call, method);
                    return;
                }
            }
            
            // Fallback to runtime helper for spread or method not found
            // We need the type name string
            string typeName = "";
            if (staticAccess.Target is QualifiedNameNode targetQname)
            {
                var parts = new List<string>();
                foreach (var p in targetQname.Parts)
                    parts.Add(p.TextValue(in source));
                typeName = string.Join("\\", parts);
            }
            else if (staticAccess.Target is IdentifierNode id && id.Token.TextValue(in source).Equals("self", StringComparison.OrdinalIgnoreCase))
            {
                if (_currentType != null)
                    typeName = _currentType.Name.Replace(".", "\\");
                else
                    throw new Exception("'self' used outside of class context.");
            }
            else
            {
                // For dynamic type (e.g. $class::method()), we can't resolve type name at compile time
                throw new NotImplementedException($"Static method call on dynamic type not supported");
            }

            Emit(OpCodes.Ldstr, typeName);
            Emit(OpCodes.Ldstr, methodName);
            var callMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("CallStaticMethodByName", new[] { typeof(string), typeof(string), typeof(object[]) })!;
            
            // Emit argument count
            Emit(OpCodes.Ldc_I4, node.Args.Count);
            Emit(OpCodes.Newarr, typeof(object));
            
            // Store args to array
            for (int i = 0; i < node.Args.Count; i++)
            {
                Emit(OpCodes.Dup);
                Emit(OpCodes.Ldc_I4, i);
                node.Args[i].Accept(this, source);
                EmitBoxingIfLiteral(node.Args[i]);
                Emit(OpCodes.Stelem_Ref);
            }
            
            Emit(OpCodes.Call, callMethod);
            return;
        }
        // Variable call (e.g., $handle(...) or $myCallable(...)) - means we're calling a Closure
        else if (node.Callee is VariableNode)
        {
            var varName = ((VariableNode)node.Callee).Token.TextValue(in source);
            
            // First push the variable (which should be a Closure)
            node.Callee.Accept(this, source);

            // Then push arguments as object array
            Emit(OpCodes.Ldc_I4, node.Args.Count);
            Emit(OpCodes.Newarr, typeof(object));

            for (int i = 0; i < node.Args.Count; i++)
            {
                Emit(OpCodes.Dup);
                Emit(OpCodes.Ldc_I4, i);
                node.Args[i].Accept(this, source);
                EmitBoxingIfLiteral(node.Args[i]);
                Emit(OpCodes.Stelem_Ref);
            }

            // Call Closure.Invoke(object[] args)
            var invokeMethod = typeof(PHPIL.Engine.Runtime.Closure).GetMethod("Invoke", new[] { typeof(object[]) })!;
            Emit(OpCodes.Callvirt, invokeMethod);
            return;
        }
        // QualifiedName call (e.g., $handle(...) - could be a closure stored under a name)
        else if (node.Callee is QualifiedNameNode qname)
        {
            var parts = new List<string>();
            foreach (var p in qname.Parts)
                parts.Add(p.TextValue(in source));
            var qnameStr = string.Join("\\", parts);
            
            // Try to resolve as a function first
            var phpFunc2 = ResolveFunction(node.Callee, in source);
            if (phpFunc2 != null)
            {
                // For include/require functions, execute immediately during compilation
                if (phpFunc2.Name is "require_once" or "require" or "include" or "include_once")
                {
                    ExecuteIncludeFunction(node, phpFunc2, source);
                    return;
                }
                // It's a named function, use the normal path
                ResolveParamsAndCall(node, phpFunc2, source);
                return;
            }
            
            // If not found as a function, treat as a variable-based closure call
            // For now, throw an error
            var (line, column) = LineColumnHelper.GetLineAndColumn(in source, qname.Parts[0]);
            throw new FunctionNotDefinedException(qnameStr, _fileName, line, column);
        }
        // Regular function call
        var phpFunc = ResolveFunction(node.Callee, in source);
        if (phpFunc == null)
        {
            string name = "unknown";
            if (node.Callee is IdentifierNode id)
            {
                name = id.Token.TextValue(in source);
            }
            else if (node.Callee is QualifiedNameNode qn)
            {
                var parts = new List<string>();
                foreach (var p in qn.Parts)
                    parts.Add(p.TextValue(in source));
                name = string.Join("\\", parts);
            }
            throw new NotImplementedException($"The function {name} is not implemented yet");
        }
        // For include/require functions, execute immediately during compilation
        if (phpFunc.Name is "require_once" or "require" or "include" or "include_once")
        {
            ExecuteIncludeFunction(node, phpFunc, source);
            return;
        }
        ResolveParamsAndCall(node, phpFunc, source);
    }

    /// <summary>
    /// Executes a PHP file inclusion function (<c>require</c>, <c>require_once</c>,
    /// <c>include</c>, or <c>include_once</c>) at compile time when the path is a string literal,
    /// or falls back to a runtime call when the path is dynamic.
    /// </summary>
    /// <param name="node">The <see cref="FunctionCallNode"/> representing the include/require call.</param>
    /// <param name="phpFunc">The resolved <see cref="PhpFunction"/> for the inclusion function.</param>
    /// <param name="source">The original source text, used to extract the file path literal.</param>
    /// <exception cref="Exception">Thrown when no argument is supplied to the inclusion function.</exception>
    /// <remarks>
    /// If the path argument is not a <see cref="LiteralNode"/>, the call is deferred to
    /// <see cref="ResolveParamsAndCall"/> for runtime resolution. If a
    /// <c>DieException</c> is raised during compile-time file execution, it is re-thrown to
    /// propagate the termination signal.
    /// </remarks>
    private void ExecuteIncludeFunction(FunctionCallNode node, PhpFunction phpFunc, ReadOnlySpan<char> source)
    {
        var arg = node.Args.FirstOrDefault();
        if (arg == null)
            throw new Exception("include/require requires a string argument");

        // For dynamic paths (variables), we can't execute at compile time
        // Emit code to execute it at runtime instead
        if (arg is not LiteralNode)
        {
            ResolveParamsAndCall(node, phpFunc, source);
            return;
        }

        var filePath = ((LiteralNode)arg).Token.TextValue(in source).Trim('\'', '"');
        
        try
        {
            PHPIL.Engine.Runtime.Runtime.ExecuteFile(filePath);
        }
        catch (PHPIL.Engine.Runtime.Sdk.DieException)
        {
            // die() was called in the included file - propagate it
            throw;
        }
    }

    /// <summary>
    /// Resolves and emits IL for a standard PHP function call, handling spread arguments,
    /// variadic parameters, and type coercions.
    /// </summary>
    /// <param name="node">The <see cref="FunctionCallNode"/> being compiled.</param>
    /// <param name="phpFunc">The resolved <see cref="PhpFunction"/> to invoke.</param>
    /// <param name="source">The original source text, used to resolve the callee name for runtime dispatch.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="phpFunc"/> has neither a <c>MethodInfo</c> nor a delegate
    /// <c>Method</c> to call.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If any argument is a <see cref="SpreadNode"/> or the function declares a variadic
    /// (<c>object[]</c>) parameter, arguments are packed into an <c>object[]</c> and dispatched
    /// via <c>RuntimeHelpers.CallVariadicFunction</c> or <c>RuntimeHelpers.CallFunctionWithSpread</c>
    /// respectively. Spread arguments are pushed as-is; all others are boxed if necessary.
    /// </para>
    /// <para>
    /// For regular calls, each argument is emitted and coerced to the corresponding parameter type
    /// via <c>EmitCoercion</c>. After the call, non-<see langword="void"/> return values are
    /// coerced to <see cref="object"/> so the result is always uniformly boxed on the stack.
    /// </para>
    /// </remarks>
    private void ResolveParamsAndCall(FunctionCallNode node, PhpFunction phpFunc, ReadOnlySpan<char> source)
    {
        // Check if any argument is a spread OR if the function has a variadic parameter
        bool hasSpread = node.Args.Any(arg => arg is SpreadNode);
        bool isVariadicFunction = phpFunc.ParameterTypes != null && phpFunc.ParameterTypes.Any(t => t == typeof(object[]));

        if (hasSpread || isVariadicFunction)
        {
            // Use runtime helper for spread arguments or variadic functions
            var methodName = "unknown";
            if (node.Callee is IdentifierNode id)
            {
                methodName = id.Token.TextValue(in source);
            }
            else if (node.Callee is QualifiedNameNode qn)
            {
                var parts = new List<string>();
                foreach (var p in qn.Parts)
                    parts.Add(p.TextValue(in source));
                methodName = string.Join("\\", parts);
            }

            // Push function name
            Emit(OpCodes.Ldstr, methodName);

            // Create array for arguments
            Emit(OpCodes.Ldc_I4, node.Args.Count);
            Emit(OpCodes.Newarr, typeof(object));

            // Store each argument to array
            for (int i = 0; i < node.Args.Count; i++)
            {
                Emit(OpCodes.Dup);
                Emit(OpCodes.Ldc_I4, i);
                
                if (node.Args[i] is SpreadNode spreadNode)
                {
                    // For spread, push the array itself
                    spreadNode.Expression.Accept(this, source);
                }
                else
                {
                    node.Args[i].Accept(this, source);
                    EmitBoxingIfLiteral(node.Args[i]);
                }
                
                Emit(OpCodes.Stelem_Ref);
            }

            var callMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod(isVariadicFunction ? "CallVariadicFunction" : "CallFunctionWithSpread", new[] { typeof(string), typeof(object[]) })!;
            Emit(OpCodes.Call, callMethod);
            return;
        }

        // Regular argument handling (no spread)
        for (int i = 0; i < node.Args.Count; i++)
        {
            node.Args[i].Accept(this, in source);
            EmitCoercion(node.Args[i].AnalysedType, phpFunc.ParameterTypes![i]);
        }

        var methodToCall = phpFunc.MethodInfo ?? phpFunc.Method?.Method;
        if (methodToCall is null)
            throw new InvalidOperationException("The PHP function doesn't have a method?");

        var returnType = methodToCall.ReturnType;

        Emit(OpCodes.Call, methodToCall);

        if (returnType != typeof(void))
        {
            AnalysedType fromAnalysedType = returnType == typeof(int) ? AnalysedType.Int :
                returnType == typeof(bool) ? AnalysedType.Boolean :
                returnType == typeof(double) ? AnalysedType.Float :
                returnType == typeof(string) ? AnalysedType.String :
                AnalysedType.Mixed;

            EmitCoercion(fromAnalysedType, typeof(object));
        }
    }

    /// <summary>
    /// Emits IL to evaluate PHP's <c>isset()</c> construct, returning a boxed <see cref="bool"/>
    /// indicating whether all supplied arguments are set and non-null.
    /// </summary>
    /// <param name="node">The <see cref="FunctionCallNode"/> representing the <c>isset()</c> call.</param>
    /// <param name="source">The original source text, used to resolve variable and array key names.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>RuntimeHelpers.GetArrayElementForIsset</c> or <c>RuntimeHelpers.IssetHelper</c>
    /// cannot be located via reflection.
    /// </exception>
    /// <remarks>
    /// <para>
    /// With no arguments, <see langword="false"/> is pushed immediately. Otherwise, all arguments
    /// are evaluated into an <c>object[]</c> using the following per-argument rules:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b>Superglobals</b> (<c>$_GET</c>, <c>$_POST</c>, etc.) — loaded via
    ///       <c>GlobalState.GetSuperglobal</c>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Known locals</b> — loaded directly from their <c>LocalBuilder</c> slot.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Undeclared variables</b> — <see langword="null"/> is pushed, matching PHP's
    ///       behaviour for unset variables.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Array access expressions</b> — the array and key are each evaluated (with value-type
    ///       keys boxed), then passed to <c>RuntimeHelpers.GetArrayElementForIsset</c> which returns
    ///       <see langword="null"/> for missing keys rather than throwing.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>All other expressions</b> — evaluated normally via <c>Accept</c>.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The populated array is passed to <c>RuntimeHelpers.IssetHelper</c>, whose result is boxed
    /// as <see cref="bool"/> and left on the stack.
    /// </para>
    /// </remarks>
    private void EmitIsset(FunctionCallNode node, ReadOnlySpan<char> source)
    {
        // isset() returns true if all arguments are set and not null
        if (node.Args.Count == 0)
        {
            Emit(OpCodes.Ldc_I4_0);
            Emit(OpCodes.Box, typeof(bool));
            return;
        }

        // Create array to hold all argument values
        Emit(OpCodes.Ldc_I4, node.Args.Count);
        Emit(OpCodes.Newarr, typeof(object));

        for (int i = 0; i < node.Args.Count; i++)
        {
            Emit(OpCodes.Dup);
            Emit(OpCodes.Ldc_I4, i);
            
            var arg = node.Args[i];

            if (arg is VariableNode varNode)
            {
                var varName = varNode.Token.TextValue(in source);
                var superglobals = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "$_GET", "$_POST", "$_COOKIE", "$_SERVER", "$_REQUEST", "$_FILES", "$_ENV", "$_SESSION"
                };

                if (superglobals.Contains(varName))
                {
                    Emit(OpCodes.Ldstr, varName);
                    var getMethod = typeof(PHPIL.Engine.Runtime.GlobalState).GetMethod("GetSuperglobal", new[] { typeof(string) });
                    Emit(OpCodes.Call, getMethod);
                }
                else if (_locals.TryGetValue(varName, out var local))
                {
                    Emit(OpCodes.Ldloc, local);
                }
                else
                {
                    Emit(OpCodes.Ldnull);
                }
            }
            else if (arg is ArrayAccessNode arrayAccess)
            {
                // Load the array
                if (arrayAccess.Array is VariableNode arrVarNode)
                {
                    var arrVarName = arrVarNode.Token.TextValue(in source);
                    if (_locals.TryGetValue(arrVarName, out var arrLocal))
                    {
                        Emit(OpCodes.Ldloc, arrLocal);
                    }
                    else
                    {
                        Emit(OpCodes.Ldnull);
                    }
                }
                else
                {
                    arrayAccess.Array.Accept(this, source);
                }

                // Load the key and ensure it's boxed to object
                if (arrayAccess.Key != null)
                {
                    arrayAccess.Key.Accept(this, source);
                    
                    // Box value types produced by VisitLiteralNode to satisfy object parameter
                    var keyType = arrayAccess.Key.AnalysedType;
                    if (keyType == AnalysedType.Int || keyType == AnalysedType.Boolean)
                    {
                        Emit(OpCodes.Box, typeof(int));
                    }
                    else if (keyType == AnalysedType.Float)
                    {
                        Emit(OpCodes.Box, typeof(double));
                    }
                    // String, Array, Object, Mixed are reference types — no boxing needed
                }
                else
                {
                    Emit(OpCodes.Ldnull);
                }

                var getElementMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("GetArrayElementForIsset", new[] { typeof(object), typeof(object) });
                if (getElementMethod == null)
                    throw new InvalidOperationException("GetArrayElementForIsset method not found in RuntimeHelpers");
                
                Emit(OpCodes.Call, getElementMethod);
            }
            else
            {
                arg.Accept(this, source);
            }
            
            Emit(OpCodes.Stelem_Ref);
        }

        var issetMethod = typeof(PHPIL.Engine.Runtime.Sdk.RuntimeHelpers).GetMethod("IssetHelper", new[] { typeof(object[]) });
        if (issetMethod == null)
            throw new InvalidOperationException("IssetHelper method not found in RuntimeHelpers");
        
        Emit(OpCodes.Call, issetMethod);
        Emit(OpCodes.Box, typeof(bool));
    }
}