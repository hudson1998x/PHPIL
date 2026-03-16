using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    /// <summary>
    /// Emits IL for a PHP function declaration, compiling the function body into a
    /// <see cref="System.Reflection.Emit.DynamicMethod"/> and registering it with the
    /// <c>FunctionTable</c>.
    /// </summary>
    /// <param name="node">The <see cref="FunctionNode"/> representing the function declaration.</param>
    /// <param name="source">The original source text, used to resolve the function name, parameter names, and return type.</param>
    /// <remarks>
    /// <para>
    /// If a namespace is active, it is prepended to the function name using backslash notation
    /// to form the fully-qualified name used for registration and resolution.
    /// </para>
    /// <para>
    /// Parameter types default to <see cref="object"/> for regular parameters and
    /// <see cref="object"/>[] for variadic parameters. The return type is resolved from the
    /// optional type hint on <paramref name="node"/>, defaulting to <see cref="object"/> when
    /// absent or unrecognised.
    /// </para>
    /// <para>
    /// A child <see cref="Compiler"/> instance is created for the function scope, inheriting the
    /// current namespace and use-imports. Each parameter is declared as a typed local and
    /// initialised from its corresponding argument via <see cref="OpCodes.Ldarg_0"/> through
    /// <see cref="OpCodes.Ldarg_3"/> or <see cref="OpCodes.Ldarg_S"/> for higher indices.
    /// </para>
    /// <para>
    /// The <see cref="PhpFunction"/> descriptor — including the unfinished
    /// <see cref="System.Reflection.Emit.DynamicMethod"/> — is registered with
    /// <c>FunctionTable</c> before the body is compiled, allowing recursive calls to resolve
    /// correctly. After the body is emitted, an implicit <see langword="null"/> return is appended
    /// for non-<see langword="void"/> functions, followed by <see cref="OpCodes.Ret"/>.
    /// </para>
    /// </remarks>
    public void VisitFunctionNode(FunctionNode node, in ReadOnlySpan<char> source)
    {
        var functionName = node.Name.TextValue(in source);
        if (!string.IsNullOrEmpty(_currentNamespace))
        {
            functionName = _currentNamespace + "\\" + functionName;
        }
        var parameterTypes = new Type[node.Params.Count];
        for (int i = 0; i < node.Params.Count; i++)
        {
            if (node.Params[i].IsVariadic)
            {
                parameterTypes[i] = typeof(object[]);
            }
            else
            {
                parameterTypes[i] = typeof(object); // Default to mixed for now
            }
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
        innerCompiler._currentFunctionName = functionName;  // Track current function name for __FUNCTION__
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

        var dynamicMethod = innerCompiler.GetDynamicMethod();

        var phpFunc = new PhpFunction
        {
            Name = functionName,
            ReturnType = returnType,
            ParameterTypes = parameterTypes,
            MethodInfo = dynamicMethod
        };

        FunctionTable.RegisterFunction(phpFunc);

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
    }

    /// <summary>
    /// Visitor stub for a function parameter declaration.
    /// </summary>
    /// <param name="node">The <see cref="FunctionParameter"/> node.</param>
    /// <param name="source">The original source text.</param>
    /// <remarks>
    /// Parameter handling is performed entirely within <see cref="VisitFunctionNode"/>; this
    /// visitor is intentionally a no-op.
    /// </remarks>
    public void VisitFunctionParameter(FunctionParameter node, in ReadOnlySpan<char> source)
    {
        // Handled directly inside VisitFunctionNode
    }
}