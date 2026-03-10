using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.SyntaxTree;

public partial class SyntaxNode
{
    /// <summary>
    /// Does this node have any type information.
    /// </summary>
    public virtual bool HasTypeEmission => false;
    
    /// <summary>
    /// Not everything will require type information
    /// </summary>
    public virtual AnalysedType AnalysedType 
    {
        get
        {
            return AnalysedType.Mixed;
        }
        set
        {
            // allowed to stay so child nodes
            // can dynamically hold type info.
        }
    }
}