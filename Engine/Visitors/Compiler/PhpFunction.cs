namespace PHPIL.Engine.Visitors;

public class PhpFunction
{
    public string? Name;

    public Type? ReturnType;

    public Delegate? Method;
    public System.Reflection.MethodInfo? MethodInfo;

    public Type[]? ParameterTypes;
}