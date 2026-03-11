namespace PHPIL.Engine.Visitors;

public class PhpFunction
{
    public string? Name;

    public Type ReturnType = typeof(void);

    public Delegate? Method;
    public System.Reflection.MethodInfo? MethodInfo;

    public Type[]? ParameterTypes;
}