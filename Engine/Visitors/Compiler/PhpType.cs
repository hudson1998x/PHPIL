using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure.OOP;

namespace PHPIL.Engine.Visitors;

public class PhpType
{
    public string Name { get; set; }
    public Type? RuntimeType { get; set; }
    public ClassNode? Definition { get; set; }
    public InterfaceNode? InterfaceDefinition { get; set; }
    public TraitNode? TraitDefinition { get; set; }
    
    // Store field builders for access during method compilation
    public Dictionary<string, FieldBuilder> FieldBuilders { get; } = new Dictionary<string, FieldBuilder>(StringComparer.OrdinalIgnoreCase);
    
    public bool IsInterface => InterfaceDefinition != null;
    public bool IsTrait => TraitDefinition != null;
    public bool IsClass => Definition != null;

    /// <summary>Returns the underlying AST node — TraitNode, ClassNode, or InterfaceNode.</summary>
    // Normalize to a common base type to satisfy the ?? operator when mixing different node kinds
    public SyntaxNode? Ast => (SyntaxNode?)TraitDefinition ?? (SyntaxNode?)Definition ?? (SyntaxNode?)InterfaceDefinition;
}
