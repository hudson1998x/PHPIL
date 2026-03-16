using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Visitors;

/// <summary>
/// Represents a compiled PHP type (class, interface, or trait), holding both the AST
/// definition used during compilation and the finished CLR <see cref="Type"/> produced
/// after <c>TypeBuilder.CreateType</c> completes.
/// </summary>
public class PhpType
{
    /// <summary>The fully-qualified PHP name of the type, using backslash namespace separators.</summary>
    public string Name { get; set; }

    /// <summary>
    /// The finished CLR <see cref="Type"/> produced by <c>TypeBuilder.CreateType</c>, or
    /// <see langword="null"/> before the type has been fully compiled.
    /// </summary>
    public Type? RuntimeType { get; set; }

    /// <summary>The AST node for the class declaration, or <see langword="null"/> if this is not a class.</summary>
    public ClassNode? Definition { get; set; }

    /// <summary>The AST node for the interface declaration, or <see langword="null"/> if this is not an interface.</summary>
    public InterfaceNode? InterfaceDefinition { get; set; }

    /// <summary>The AST node for the trait declaration, or <see langword="null"/> if this is not a trait.</summary>
    public TraitNode? TraitDefinition { get; set; }

    /// <summary>
    /// Maps property names (case-insensitively) to their <see cref="FieldBuilder"/> slots,
    /// populated during Pass 1 of <see cref="Compiler.VisitClassNode"/> so that method bodies
    /// can emit direct <c>ldfld</c>/<c>stfld</c> instructions rather than going through reflection.
    /// </summary>
    public Dictionary<string, FieldBuilder> FieldBuilders { get; } = new Dictionary<string, FieldBuilder>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether this type represents a PHP interface.</summary>
    public bool IsInterface => InterfaceDefinition != null;

    /// <summary>Gets a value indicating whether this type represents a PHP trait.</summary>
    public bool IsTrait => TraitDefinition != null;

    /// <summary>Gets a value indicating whether this type represents a PHP class.</summary>
    public bool IsClass => Definition != null;

    /// <summary>
    /// Returns the underlying AST node regardless of type kind — one of <see cref="TraitNode"/>,
    /// <see cref="ClassNode"/>, or <see cref="InterfaceNode"/> — normalised to
    /// <see cref="SyntaxNode"/> to satisfy the <c>??</c> operator when mixing node kinds.
    /// </summary>
    public SyntaxNode? Ast => (SyntaxNode?)TraitDefinition ?? (SyntaxNode?)Definition ?? (SyntaxNode?)InterfaceDefinition;
}