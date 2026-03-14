using System.Reflection;
using System.Reflection.Emit;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors;

public partial class Compiler : IVisitor
{
    private string _currentNamespace = "";
    private readonly Dictionary<string, string> _useImports = [];
    private Type? _currentType;
    private bool _isStaticMethod;

    public void Visit(SyntaxNode node, in ReadOnlySpan<char> span)
    {
        throw new NotImplementedException();
    }



    public void VisitArgumentListNode(ArgumentListNode node, in ReadOnlySpan<char> source)
    {
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this, source);
        }
    }

    public void VisitElseIfNode(ElseIfNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitGroupNode(GroupNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitIdentifierNode(IdentifierNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }


    public void VisitElseNode(ElseNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }

    public void VisitSyntaxNode(SyntaxNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }


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