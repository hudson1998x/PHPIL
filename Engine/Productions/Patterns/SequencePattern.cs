using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class SequencePattern : Pattern
{
    private readonly (Pattern Pattern, Action<SyntaxNode>? Callback)[] _steps;

    public SequencePattern((Pattern, Action<SyntaxNode>?)[] steps) => _steps = steps;

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int startPos = ctx.Save();
        result = null; // Sequence itself doesn't usually produce a single node unless specialized

        foreach (var (pattern, callback) in _steps)
        {
            if (pattern.TryMatch(ref ctx, out var matchedNode))
            {
                if (matchedNode != null) callback?.Invoke(matchedNode);
            }
            else
            {
                ctx.Restore(startPos);
                return false;
            }
        }
        return true;
    }
}