using PHPIL.Engine.Visitors;

namespace PHPIL.Engine.Runtime.Sdk;

public static class Sdk
{
    public static SdkFunctionBuilder Function(string name)
    {
        return new SdkFunctionBuilder(name);
    }
}

public class SdkFunctionBuilder
{
    private readonly string _name;
    private Type[]? _paramTypes;
    private Type _returnType = typeof(void);
    private Delegate? _method;

    public SdkFunctionBuilder(string name)
    {
        _name = name;
    }

    public SdkFunctionBuilder Returns<T>()
    {
        _returnType = typeof(T);
        return this;
    }

    public SdkFunctionBuilder Takes<T1>()
    {
        _paramTypes = [typeof(T1)];
        return this;
    }

    public SdkFunctionBuilder Takes<T1, T2>()
    {
        _paramTypes = [typeof(T1), typeof(T2)];
        return this;
    }

    public SdkFunctionBuilder Takes<T1, T2, T3>()
    {
        _paramTypes = [typeof(T1), typeof(T2), typeof(T3)];
        return this;
    }

    public SdkFunctionBuilder Takes<T1, T2, T3, T4>()
    {
        _paramTypes = [typeof(T1), typeof(T2), typeof(T3), typeof(T4)];
        return this;
    }

    public SdkFunctionBuilder Takes(params Type[] types)
    {
        _paramTypes = types;
        return this;
    }

    public SdkFunctionBuilder Calls(Delegate method)
    {
        _method = method;
        Register();
        return this;
    }

    public SdkFunctionBuilder Calls<T>(Func<T> method)
    {
        _method = method;
        _returnType = typeof(T);
        Register();
        return this;
    }

    public SdkFunctionBuilder Calls<T1, T>(Func<T1, T> method)
    {
        _method = method;
        _returnType = typeof(T);
        Register();
        return this;
    }

    public SdkFunctionBuilder Calls<T1, T2, T>(Func<T1, T2, T> method)
    {
        _method = method;
        _returnType = typeof(T);
        Register();
        return this;
    }

    public SdkFunctionBuilder Calls<T1, T2, T3, T>(Func<T1, T2, T3, T> method)
    {
        _method = method;
        _returnType = typeof(T);
        Register();
        return this;
    }

    public SdkFunctionBuilder Calls<T1>(Action<T1> method)
    {
        _method = method;
        _returnType = typeof(void);
        Register();
        return this;
    }

    public SdkFunctionBuilder Calls<T1, T2>(Action<T1, T2> method)
    {
        _method = method;
        _returnType = typeof(void);
        Register();
        return this;
    }

    private void Register()
    {
        var phpFunc = new PhpFunction
        {
            Name = _name,
            ParameterTypes = _paramTypes ?? [],
            ReturnType = _returnType,
            Method = _method
        };
        FunctionTable.RegisterFunction(phpFunc);
    }
}
