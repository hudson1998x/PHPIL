namespace PHPIL.Engine.Runtime.Types;

public delegate object PhpCallable(params PhpValue[] args);

public class PhpFunction
{
    public string? Name;

    public bool IsSystem = false;

    public bool IsCompiled = false;

    public PhpCallable? Action;
    
    public PhpValue? ReturnType;
}