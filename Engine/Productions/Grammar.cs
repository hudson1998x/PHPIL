namespace PHPIL.Engine.Productions;

using PHPIL.Engine.Productions.Patterns;

public static partial class Grammar
{
    public static NamespacePattern NamespaceDeclaration() => new NamespacePattern();
    public static QualifiedNamePattern QualifiedName() => new QualifiedNamePattern();
    public static UsePattern Use() => new UsePattern();
}