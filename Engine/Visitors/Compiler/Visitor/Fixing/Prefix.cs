using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Engine.Visitors;

public partial class Compiler
{
    public void VisitPrefixExpressionNode(PrefixExpressionNode node, in ReadOnlySpan<char> source)
    {
        throw new NotImplementedException();
    }
}