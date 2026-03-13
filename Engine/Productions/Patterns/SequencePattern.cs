using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Productions.Patterns;

public class SequencePattern : Pattern
{
    private readonly (Pattern Pattern, Action<SyntaxNode>? Callback)[] _steps;

    public SequencePattern((Pattern, Action<SyntaxNode>?)[] steps) => _steps = steps;

    public override bool TryMatch(ref ParserContext ctx, out SyntaxNode? result)
    {
        int startPos = ctx.Save();
        result = null;

        for (int i = 0; i < _steps.Length; i++)
        {
            var (pattern, callback) = _steps[i];
            if (pattern.TryMatch(ref ctx, out var matchedNode))
            {
                if (matchedNode != null) callback?.Invoke(matchedNode);
            }
            else
            {
                // Record failure at the step that failed
                var current = ctx.Peek();
                ctx.RecordFailure(
                    ctx.Position, 
                    $"Sequence at step {i}", 
                    $"token for step {i}"
                );
                ctx.Restore(startPos);
                return false;
            }
        }
        return true;
    }
}