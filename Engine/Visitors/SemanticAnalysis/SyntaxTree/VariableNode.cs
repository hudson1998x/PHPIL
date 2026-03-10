using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.SyntaxTree;

public partial class VariableNode
{
    public override bool HasTypeEmission => true;

    private AnalysedType _variableType = AnalysedType.Mixed;
    
    public override AnalysedType AnalysedType
    {
        set { _variableType = value; }
        get { return _variableType; }
    }
}