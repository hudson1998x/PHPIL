using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitArrayAccessNode(ArrayAccessNode node, in ReadOnlySpan<char> source)
    {
        node.Array.Accept(this, source);
        node.Key?.Accept(this, source);
        node.AnalysedType = SemanticAnalysis.AnalysedType.Mixed; 
    }
}
