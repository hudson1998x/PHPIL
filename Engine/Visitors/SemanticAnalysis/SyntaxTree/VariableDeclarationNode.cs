using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableDeclaration
{
    public override bool HasTypeEmission => true;

    public override AnalysedType AnalysedType { get; set; }
}