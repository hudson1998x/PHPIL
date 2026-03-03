using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Productions;

public static partial class Grammar
{
    public static Pattern ParameterList() => new ParameterListPattern();
    public static Pattern UseList() => new UseListPattern();

    public static Pattern FunctionDeclaration() => new FunctionDeclarationPattern();
    
    public static Pattern AnonymousFunction() => new AnonymousFunctionPattern();
}