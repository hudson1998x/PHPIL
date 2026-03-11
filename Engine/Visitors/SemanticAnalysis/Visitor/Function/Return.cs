using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Visitors.SemanticAnalysis;

public partial class SemanticVisitor
{
    public void VisitReturnNode(ReturnNode node, in ReadOnlySpan<char> source)
    {
        node.Expression?.Accept(this, source);
    }
}