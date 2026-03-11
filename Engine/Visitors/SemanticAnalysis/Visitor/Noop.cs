using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitBreakNode(BreakNode node, in ReadOnlySpan<char> source)
    {
        
    }
    
    public void VisitContinueNode(ContinueNode node, in ReadOnlySpan<char> source)
    {
        // nothing to analyse
    }
}