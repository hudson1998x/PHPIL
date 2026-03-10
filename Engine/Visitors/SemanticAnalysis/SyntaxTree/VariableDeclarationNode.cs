using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableDeclaration
{
    public override bool HasTypeEmission => true;

    public override AnalysedType AnalysedType { get; set; }

    public bool IsCaptured { get; set; } = false;
    
    public bool IsUsed { get; set; } = false;

    public bool EmitValue => VariableValue is VariableDeclaration;
}