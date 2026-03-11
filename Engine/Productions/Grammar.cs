namespace PHPIL.Engine.Productions;

using PHPIL.Engine.Productions.Patterns;

public static partial class Grammar
{
    public static NamespacePattern NamespaceDeclaration() => new NamespacePattern();
    public static QualifiedNamePattern QualifiedName() => new QualifiedNamePattern();
    public static UsePattern Use() => new UsePattern();
    public static ClassPattern ClassDeclaration() => new ClassPattern();
    public static InterfacePattern InterfaceDeclaration() => new InterfacePattern();
    public static TraitPattern TraitDeclaration() => new TraitPattern();
    public static ConstantPattern ConstantDeclaration() => new ConstantPattern();
    public static NewPattern NewExpression() => new NewPattern();
}