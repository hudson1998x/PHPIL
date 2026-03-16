using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
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

    public void VisitFunctionParameter(FunctionParameter node, in ReadOnlySpan<char> source)
    {
        // Handled directly inside VisitFunctionNode
    }
}
