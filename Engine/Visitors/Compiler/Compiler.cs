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
        // Determine parameter types
        var parameterTypes = new Type[node.Params.Count];
        for (int i = 0; i < node.Params.Count; i++)
        {
            // For now, all parameters are object (mixed)
            parameterTypes[i] = typeof(object);
            // TODO: handle type hints from node.Params[i].TypeHint if any
        }

        // Determine return type
        var returnType = typeof(object); // Default to mixed for now
        if (node.ReturnType != null)
        {
            var typeName = node.ReturnType.Value.TextValue(in source);
            returnType = typeName switch
            {
                "int" => typeof(int),
                "float" or "double" => typeof(double),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "void" => typeof(void),
                _ => typeof(object)
            };
        }

        // Create inner compiler for the function body
        var methodName = "phpil_anon_" + Guid.NewGuid().ToString("N");
        var innerCompiler = new Compiler(methodName, returnType, parameterTypes);
        innerCompiler._currentNamespace = _currentNamespace;
        foreach (var import in _useImports)
            innerCompiler._useImports[import.Key] = import.Value;

        // Load parameters into locals (so the body can access them by name)
        for (int i = 0; i < node.Params.Count; i++)
        {
            var paramName = node.Params[i].Name.TextValue(in source);
            var local = innerCompiler.DeclareLocal(parameterTypes[i]);
            innerCompiler._locals[paramName] = local;

            // Emit Ldarg_i and store to local
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

        // Get the dynamic method
        var dynamicMethod = innerCompiler.GetDynamicMethod();
        if (dynamicMethod == null)
        {
            throw new InvalidOperationException("Failed to create dynamic method for anonymous function");
        }

        // Create delegate type
        Type delegateType;
        if (returnType == typeof(void))
        {
            if (parameterTypes.Length == 0)
                delegateType = typeof(Action);
            else if (parameterTypes.Length == 1)
                delegateType = typeof(Action<>).MakeGenericType(parameterTypes[0]);
            else if (parameterTypes.Length == 2)
                delegateType = typeof(Action<,>).MakeGenericType(parameterTypes[0], parameterTypes[1]);
            else if (parameterTypes.Length == 3)
                delegateType = typeof(Action<,,>).MakeGenericType(parameterTypes[0], parameterTypes[1], parameterTypes[2]);
            else
                delegateType = typeof(MulticastDelegate); // Fallback
        }
        else
        {
            if (parameterTypes.Length == 0)
                delegateType = typeof(Func<>).MakeGenericType(returnType);
            else if (parameterTypes.Length == 1)
                delegateType = typeof(Func<,>).MakeGenericType(parameterTypes[0], returnType);
            else if (parameterTypes.Length == 2)
                delegateType = typeof(Func<,,>).MakeGenericType(parameterTypes[0], parameterTypes[1], returnType);
            else if (parameterTypes.Length == 3)
                delegateType = typeof(Func<,,,>).MakeGenericType(parameterTypes[0], parameterTypes[1], parameterTypes[2], returnType);
            else
                delegateType = typeof(MulticastDelegate); // Fallback
        }

        // Get the delegate constructor that takes (object target, IntPtr method)
        ConstructorInfo? ctor = null;
        if (delegateType != typeof(MulticastDelegate))
        {
            ctor = delegateType.GetConstructor(new[] { typeof(object), typeof(IntPtr) });
        }

        if (ctor == null)
        {
            // Fallback - just push null or something
            Emit(OpCodes.Ldnull);
            return;
        }

        // Emit null for target (static method)
        Emit(OpCodes.Ldnull);
        
        // Emit method pointer for our dynamic method
        Emit(OpCodes.Ldftn, dynamicMethod);
        
        // Create delegate instance
        Emit(OpCodes.Newobj, ctor);
    }


}